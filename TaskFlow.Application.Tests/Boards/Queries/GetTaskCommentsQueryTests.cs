using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Boards.Queries;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Tests.TestDoubles;
using TaskFlow.Domain.Boards;
using TaskFlow.Domain.Projects;
using TaskFlow.Infrastructure.Persistence;
using Xunit;

namespace TaskFlow.Application.Tests.Boards.Queries;

public class GetTaskCommentsQueryTests
{
    private readonly DbContextOptions<AppDbContext> _options;

    public GetTaskCommentsQueryTests()
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
        task.AddComment(ownerId, "コメント1");
        task.AddComment(ownerId, "コメント2");

        await using var ctx = CreateContext();
        var repo = new BoardRepository(ctx);
        await new ProjectRepository(ctx).AddAsync(project);
        await repo.AddAsync(board);
        await repo.AddColumnAsync(column);
        await repo.AddTaskAsync(task);  // コメント2件もカスケード保存される
        return (project, task, ownerId);
    }

    [Fact]
    public async Task HandleAsync_コメント取得_正しい件数が返される()
    {
        var (_, task, ownerId) = await SeedAsync();
        var query = new GetTaskCommentsQuery(task.Id, ownerId);

        await using var ctx = CreateContext();
        var result = await new GetTaskCommentsQueryHandler(
            new BoardRepository(ctx),
            new ProjectRepository(ctx),
            new FakeIdentityService()).HandleAsync(query);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task HandleAsync_コメント取得_内容が正しい()
    {
        var (_, task, ownerId) = await SeedAsync();
        var query = new GetTaskCommentsQuery(task.Id, ownerId);

        await using var ctx = CreateContext();
        var result = await new GetTaskCommentsQueryHandler(
            new BoardRepository(ctx),
            new ProjectRepository(ctx),
            new FakeIdentityService()).HandleAsync(query);

        Assert.Contains(result, c => c.Body == "コメント1");
        Assert.Contains(result, c => c.Body == "コメント2");
    }

    [Fact]
    public async Task HandleAsync_コメントなし_空リストを返す()
    {
        var ownerId = Guid.NewGuid();
        var project = Project.Create("プロジェクト", "説明", ownerId);
        var board = Board.Create(project.Id, "ボード");
        var column = Column.Create(board.Id, "ToDo", 0);
        var task = BoardTask.Create(column.Id, "タスク", "説明", null, 0);

        await using var ctx = CreateContext();
        var repo = new BoardRepository(ctx);
        await new ProjectRepository(ctx).AddAsync(project);
        await repo.AddAsync(board);
        await repo.AddColumnAsync(column);
        await repo.AddTaskAsync(task);

        await using var ctx2 = CreateContext();
        var result = await new GetTaskCommentsQueryHandler(
            new BoardRepository(ctx2),
            new ProjectRepository(ctx2),
            new FakeIdentityService()).HandleAsync(new GetTaskCommentsQuery(task.Id, ownerId));

        Assert.Empty(result);
    }

    [Fact]
    public async Task HandleAsync_存在しないタスク_NotFoundExceptionをスロー()
    {
        var query = new GetTaskCommentsQuery(Guid.NewGuid(), Guid.NewGuid());

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<NotFoundException>(
            () => new GetTaskCommentsQueryHandler(
                new BoardRepository(ctx),
                new ProjectRepository(ctx),
                new FakeIdentityService()).HandleAsync(query));
    }

    [Fact]
    public async Task HandleAsync_非メンバーが取得_ForbiddenAccessExceptionをスロー()
    {
        var (_, task, _) = await SeedAsync();
        var query = new GetTaskCommentsQuery(task.Id, Guid.NewGuid());

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => new GetTaskCommentsQueryHandler(
                new BoardRepository(ctx),
                new ProjectRepository(ctx),
                new FakeIdentityService()).HandleAsync(query));
    }
}
