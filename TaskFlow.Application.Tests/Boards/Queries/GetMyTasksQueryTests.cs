using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Boards.Queries;
using TaskFlow.Domain.Boards;
using TaskFlow.Domain.Projects;
using TaskFlow.Infrastructure.Persistence;
using Xunit;

namespace TaskFlow.Application.Tests.Boards.Queries;

public class GetMyTasksQueryTests
{
    private readonly DbContextOptions<AppDbContext> _options;

    public GetMyTasksQueryTests()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private AppDbContext CreateContext() => new(_options);

    [Fact]
    public async Task HandleAsync_担当タスクあり_プロジェクト名と列名付きで返る()
    {
        var userId = Guid.NewGuid();
        var project = Project.Create("プロジェクトA", "説明", userId);
        var board = Board.Create(project.Id, "ボード");
        var column = Column.Create(board.Id, "ToDo", 0);
        var task = BoardTask.Create(column.Id, "自分のタスク", "説明", userId, 0);

        await using (var ctx = CreateContext())
        {
            var repo = new BoardRepository(ctx);
            await new ProjectRepository(ctx).AddAsync(project);
            await repo.AddAsync(board);
            await repo.AddColumnAsync(column);
            await repo.AddTaskAsync(task);
        }

        await using var queryCtx = CreateContext();
        var result = await new GetMyTasksQueryHandler(
            new BoardRepository(queryCtx),
            new ProjectRepository(queryCtx)).HandleAsync(new GetMyTasksQuery(userId));

        Assert.Single(result);
        Assert.Equal(task.Id, result[0].Id);
        Assert.Equal("自分のタスク", result[0].Title);
        Assert.Equal(project.Id, result[0].ProjectId);
        Assert.Equal("プロジェクトA", result[0].ProjectName);
        Assert.Equal(column.Id, result[0].ColumnId);
        Assert.Equal("ToDo", result[0].ColumnName);
    }

    [Fact]
    public async Task HandleAsync_担当タスクなし_空リストを返す()
    {
        await using var ctx = CreateContext();
        var result = await new GetMyTasksQueryHandler(
            new BoardRepository(ctx),
            new ProjectRepository(ctx)).HandleAsync(new GetMyTasksQuery(Guid.NewGuid()));

        Assert.Empty(result);
    }

    [Fact]
    public async Task HandleAsync_他人の担当タスクは含まれない()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var project = Project.Create("プロジェクトB", "説明", ownerId);
        var board = Board.Create(project.Id, "ボード");
        var column = Column.Create(board.Id, "ToDo", 0);
        var myTask = BoardTask.Create(column.Id, "自分のタスク", "説明", ownerId, 0);
        var otherTask = BoardTask.Create(column.Id, "他人のタスク", "説明", otherUserId, 1);

        await using (var ctx = CreateContext())
        {
            var repo = new BoardRepository(ctx);
            await new ProjectRepository(ctx).AddAsync(project);
            await repo.AddAsync(board);
            await repo.AddColumnAsync(column);
            await repo.AddTaskAsync(myTask);
            await repo.AddTaskAsync(otherTask);
        }

        await using var queryCtx = CreateContext();
        var result = await new GetMyTasksQueryHandler(
            new BoardRepository(queryCtx),
            new ProjectRepository(queryCtx)).HandleAsync(new GetMyTasksQuery(ownerId));

        Assert.Single(result);
        Assert.Equal(myTask.Id, result[0].Id);
    }

    [Fact]
    public async Task HandleAsync_未アサインタスクは含まれない()
    {
        var userId = Guid.NewGuid();
        var project = Project.Create("プロジェクトC", "説明", userId);
        var board = Board.Create(project.Id, "ボード");
        var column = Column.Create(board.Id, "ToDo", 0);
        var unassignedTask = BoardTask.Create(column.Id, "未アサイン", "説明", null, 0);

        await using (var ctx = CreateContext())
        {
            var repo = new BoardRepository(ctx);
            await new ProjectRepository(ctx).AddAsync(project);
            await repo.AddAsync(board);
            await repo.AddColumnAsync(column);
            await repo.AddTaskAsync(unassignedTask);
        }

        await using var queryCtx = CreateContext();
        var result = await new GetMyTasksQueryHandler(
            new BoardRepository(queryCtx),
            new ProjectRepository(queryCtx)).HandleAsync(new GetMyTasksQuery(userId));

        Assert.Empty(result);
    }
}
