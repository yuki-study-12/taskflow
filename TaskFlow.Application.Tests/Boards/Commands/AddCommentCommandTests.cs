using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Boards.Commands;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Domain.Boards;
using TaskFlow.Domain.Projects;
using TaskFlow.Infrastructure.Persistence;
using Xunit;

namespace TaskFlow.Application.Tests.Boards.Commands;

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
        var task = BoardTask.Create(column.Id, "タスク1", "説明", null, 0);

        await using var ctx = CreateContext();
        var repo = new BoardRepository(ctx);
        await new ProjectRepository(ctx).AddAsync(project);
        await repo.AddAsync(board);
        await repo.AddColumnAsync(column);
        await repo.AddTaskAsync(task);
        return (project, task, ownerId);
    }

    [Fact]
    public async Task HandleAsync_正常追加_コメントが返される()
    {
        var (_, task, ownerId) = await SeedAsync();
        var command = new AddCommentCommand(task.Id, "テストコメント", ownerId);

        await using var ctx = CreateContext();
        var result = await new AddCommentCommandHandler(
            new BoardRepository(ctx),
            new ProjectRepository(ctx)).HandleAsync(command);

        Assert.Equal("テストコメント", result.Body);
        Assert.Equal(task.Id, result.TaskId);
        Assert.Equal(ownerId, result.AuthorId);
    }

    [Fact]
    public async Task HandleAsync_正常追加_DBに保存される()
    {
        var (_, task, ownerId) = await SeedAsync();
        var command = new AddCommentCommand(task.Id, "テストコメント", ownerId);

        await using var ctx = CreateContext();
        await new AddCommentCommandHandler(
            new BoardRepository(ctx),
            new ProjectRepository(ctx)).HandleAsync(command);

        await using var verify = CreateContext();
        var comments = await new BoardRepository(verify).GetCommentsByTaskIdAsync(task.Id);
        Assert.Single(comments);
        Assert.Equal("テストコメント", comments[0].Body);
    }

    [Fact]
    public async Task HandleAsync_存在しないタスク_NotFoundExceptionをスロー()
    {
        var command = new AddCommentCommand(Guid.NewGuid(), "コメント", Guid.NewGuid());

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<NotFoundException>(
            () => new AddCommentCommandHandler(
                new BoardRepository(ctx),
                new ProjectRepository(ctx)).HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_非メンバーが追加_ForbiddenAccessExceptionをスロー()
    {
        var (_, task, _) = await SeedAsync();
        var command = new AddCommentCommand(task.Id, "コメント", Guid.NewGuid());

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => new AddCommentCommandHandler(
                new BoardRepository(ctx),
                new ProjectRepository(ctx)).HandleAsync(command));
    }
}
