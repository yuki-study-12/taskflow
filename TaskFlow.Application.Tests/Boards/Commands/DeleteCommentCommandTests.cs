using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Boards.Commands;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Domain.Boards;
using TaskFlow.Domain.Projects;
using TaskFlow.Infrastructure.Persistence;
using Xunit;

namespace TaskFlow.Application.Tests.Boards.Commands;

public class DeleteCommentCommandTests
{
    private readonly DbContextOptions<AppDbContext> _options;

    public DeleteCommentCommandTests()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private AppDbContext CreateContext() => new(_options);

    private async Task<(Project project, BoardTask task, TaskComment comment, Guid ownerId)> SeedAsync()
    {
        var ownerId = Guid.NewGuid();
        var project = Project.Create("プロジェクト", "説明", ownerId);
        var board = Board.Create(project.Id, "ボード");
        var column = Column.Create(board.Id, "ToDo", 0);
        var task = BoardTask.Create(column.Id, "タスク1", "説明", null, 0);
        var comment = task.AddComment(ownerId, "テストコメント");

        await using var ctx = CreateContext();
        var repo = new BoardRepository(ctx);
        await new ProjectRepository(ctx).AddAsync(project);
        await repo.AddAsync(board);
        await repo.AddColumnAsync(column);
        await repo.AddTaskAsync(task);  // コメントもカスケード保存される
        return (project, task, comment, ownerId);
    }

    [Fact]
    public async Task HandleAsync_正常削除_DBからコメントが消える()
    {
        var (_, task, comment, ownerId) = await SeedAsync();
        var command = new DeleteCommentCommand(comment.Id, ownerId);

        await using var ctx = CreateContext();
        await new DeleteCommentCommandHandler(new BoardRepository(ctx)).HandleAsync(command);

        await using var verify = CreateContext();
        var result = await new BoardRepository(verify).GetCommentByIdAsync(comment.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_正常削除_trueを返す()
    {
        var (_, _, comment, ownerId) = await SeedAsync();
        var command = new DeleteCommentCommand(comment.Id, ownerId);

        await using var ctx = CreateContext();
        var result = await new DeleteCommentCommandHandler(new BoardRepository(ctx)).HandleAsync(command);

        Assert.True(result);
    }

    [Fact]
    public async Task HandleAsync_存在しないコメント_NotFoundExceptionをスロー()
    {
        var command = new DeleteCommentCommand(Guid.NewGuid(), Guid.NewGuid());

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<NotFoundException>(
            () => new DeleteCommentCommandHandler(new BoardRepository(ctx)).HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_投稿者以外が削除_ForbiddenAccessExceptionをスロー()
    {
        var (_, _, comment, _) = await SeedAsync();
        var command = new DeleteCommentCommand(comment.Id, Guid.NewGuid());

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => new DeleteCommentCommandHandler(new BoardRepository(ctx)).HandleAsync(command));
    }
}
