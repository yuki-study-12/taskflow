using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Boards.Commands;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Models;
using TaskFlow.Application.Tests.TestDoubles;
using TaskFlow.Domain.Boards;
using TaskFlow.Domain.Projects;
using TaskFlow.Infrastructure.Persistence;
using Xunit;

namespace TaskFlow.Application.Tests.Notifications;

public class AddCommentCommandTests
{
    private readonly DbContextOptions<AppDbContext> _options;

    public AddCommentCommandTests()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private AppDbContext CreateContext() => new(_options);

    private async Task<(Project project, BoardTask task, Guid ownerId)> SeedAsync()
    {
        var ownerId = Guid.NewGuid();
        var project = Project.Create("プロジェクト", "説明", ownerId);
        var board = Board.Create(project.Id, "ボード");
        var column = Column.Create(board.Id, "ToDo", 0);
        var task = BoardTask.Create(column.Id, "タスク", "", null, 0, ownerId);

        await using var ctx = CreateContext();
        var boardRepo = new BoardRepository(ctx);
        await new ProjectRepository(ctx).AddAsync(project);
        await boardRepo.AddAsync(board);
        await boardRepo.AddColumnAsync(column);
        await boardRepo.AddTaskAsync(task);

        return (project, task, ownerId);
    }

    [Fact]
    public async Task HandleAsync_メンバーがコメントを追加できる()
    {
        var (_, task, ownerId) = await SeedAsync();
        var command = new AddCommentCommand(task.Id, "コメント本文", ownerId);

        await using var ctx = CreateContext();
        var handler = new AddCommentCommandHandler(new BoardRepository(ctx), new ProjectRepository(ctx), new FakeIdentityService());
        var result = await handler.HandleAsync(command);

        Assert.IsType<CommentDto>(result);
        Assert.Equal("コメント本文", result.Body);
        Assert.Equal(ownerId, result.AuthorId);
    }

    [Fact]
    public async Task HandleAsync_存在しないタスク_NotFoundExceptionをスロー()
    {
        await using var ctx = CreateContext();
        var handler = new AddCommentCommandHandler(new BoardRepository(ctx), new ProjectRepository(ctx), new FakeIdentityService());
        var command = new AddCommentCommand(Guid.NewGuid(), "本文", Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_非メンバーがコメント_ForbiddenAccessExceptionをスロー()
    {
        var (_, task, _) = await SeedAsync();
        var command = new AddCommentCommand(task.Id, "本文", Guid.NewGuid());

        await using var ctx = CreateContext();
        var handler = new AddCommentCommandHandler(new BoardRepository(ctx), new ProjectRepository(ctx), new FakeIdentityService());

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.HandleAsync(command));
    }
}

public class AddCommentCommandValidatorTests
{
    [Fact]
    public void Validate_正常な入力_バリデーション成功()
    {
        var command = new AddCommentCommand(Guid.NewGuid(), "本文", Guid.NewGuid());
        var result = new AddCommentCommandValidator().Validate(command);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_本文が空_バリデーション失敗()
    {
        var command = new AddCommentCommand(Guid.NewGuid(), "", Guid.NewGuid());
        var result = new AddCommentCommandValidator().Validate(command);
        Assert.False(result.IsValid);
    }
}
