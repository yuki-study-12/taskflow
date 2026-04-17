using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Boards.Queries;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Domain.Boards;
using TaskFlow.Domain.Projects;
using TaskFlow.Infrastructure.Persistence;
using Xunit;

namespace TaskFlow.Application.Tests.Boards.Queries;

public class GetTaskByIdQueryTests
{
    private readonly DbContextOptions<AppDbContext> _options;

    public GetTaskByIdQueryTests()
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
        var task = BoardTask.Create(column.Id, "タスク1", "詳細説明", null, 0);

        await using var ctx = CreateContext();
        var repo = new BoardRepository(ctx);
        await new ProjectRepository(ctx).AddAsync(project);
        await repo.AddAsync(board);
        await repo.AddColumnAsync(column);
        await repo.AddTaskAsync(task);
        return (project, task, ownerId);
    }

    [Fact]
    public async Task HandleAsync_正常取得_タスクが返る()
    {
        var (project, task, ownerId) = await SeedAsync();
        var query = new GetTaskByIdQuery(task.Id, ownerId);

        await using var ctx = CreateContext();
        var result = await new GetTaskByIdQueryHandler(
            new BoardRepository(ctx),
            new ProjectRepository(ctx)).HandleAsync(query);

        Assert.Equal(task.Id, result.Id);
        Assert.Equal("タスク1", result.Title);
        Assert.Equal("詳細説明", result.Description);
    }

    [Fact]
    public async Task HandleAsync_存在しないタスク_NotFoundExceptionをスロー()
    {
        var query = new GetTaskByIdQuery(Guid.NewGuid(), Guid.NewGuid());

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<NotFoundException>(
            () => new GetTaskByIdQueryHandler(
                new BoardRepository(ctx),
                new ProjectRepository(ctx)).HandleAsync(query));
    }

    [Fact]
    public async Task HandleAsync_非メンバーが取得_ForbiddenAccessExceptionをスロー()
    {
        var (_, task, _) = await SeedAsync();
        var query = new GetTaskByIdQuery(task.Id, Guid.NewGuid());

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => new GetTaskByIdQueryHandler(
                new BoardRepository(ctx),
                new ProjectRepository(ctx)).HandleAsync(query));
    }
}
