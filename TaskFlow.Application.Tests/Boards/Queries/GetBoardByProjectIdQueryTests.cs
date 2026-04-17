using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Boards.Queries;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Domain.Boards;
using TaskFlow.Domain.Projects;
using TaskFlow.Infrastructure.Persistence;
using Xunit;

namespace TaskFlow.Application.Tests.Boards.Queries;

public class GetBoardByProjectIdQueryTests
{
    private readonly DbContextOptions<AppDbContext> _options;

    public GetBoardByProjectIdQueryTests()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private AppDbContext CreateContext() => new(_options);

    private async Task<(Project project, Board board, Column column, BoardTask task, Guid ownerId)> SeedAsync()
    {
        var ownerId = Guid.NewGuid();
        var project = Project.Create("プロジェクト", "説明", ownerId);
        var board = Board.Create(project.Id, "スプリントボード");
        var column = Column.Create(board.Id, "ToDo", 0);
        var task = BoardTask.Create(column.Id, "タスク1", "説明", null, 0);

        await using var ctx = CreateContext();
        var repo = new BoardRepository(ctx);
        await new ProjectRepository(ctx).AddAsync(project);
        await repo.AddAsync(board);
        await repo.AddColumnAsync(column);
        await repo.AddTaskAsync(task);
        return (project, board, column, task, ownerId);
    }

    [Fact]
    public async Task HandleAsync_正常取得_ボードが返る()
    {
        var (project, board, _, _, ownerId) = await SeedAsync();
        var query = new GetBoardByProjectIdQuery(project.Id, ownerId);

        await using var ctx = CreateContext();
        var result = await new GetBoardByProjectIdQueryHandler(
            new BoardRepository(ctx),
            new ProjectRepository(ctx)).HandleAsync(query);

        Assert.Equal(board.Id, result.Id);
        Assert.Equal("スプリントボード", result.Name);
        Assert.Equal(project.Id, result.ProjectId);
    }

    [Fact]
    public async Task HandleAsync_列とタスクがネストされる()
    {
        var (project, _, column, task, ownerId) = await SeedAsync();
        var query = new GetBoardByProjectIdQuery(project.Id, ownerId);

        await using var ctx = CreateContext();
        var result = await new GetBoardByProjectIdQueryHandler(
            new BoardRepository(ctx),
            new ProjectRepository(ctx)).HandleAsync(query);

        Assert.Single(result.Columns);
        Assert.Equal(column.Id, result.Columns[0].Id);
        Assert.Single(result.Columns[0].Tasks);
        Assert.Equal(task.Id, result.Columns[0].Tasks[0].Id);
    }

    [Fact]
    public async Task HandleAsync_存在しないプロジェクト_NotFoundExceptionをスロー()
    {
        var query = new GetBoardByProjectIdQuery(Guid.NewGuid(), Guid.NewGuid());

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<NotFoundException>(
            () => new GetBoardByProjectIdQueryHandler(
                new BoardRepository(ctx),
                new ProjectRepository(ctx)).HandleAsync(query));
    }

    [Fact]
    public async Task HandleAsync_ボードなし_NotFoundExceptionをスロー()
    {
        var ownerId = Guid.NewGuid();
        var project = Project.Create("プロジェクト", "説明", ownerId);
        await using (var ctx = CreateContext())
            await new ProjectRepository(ctx).AddAsync(project);

        var query = new GetBoardByProjectIdQuery(project.Id, ownerId);

        await using var ctx2 = CreateContext();
        await Assert.ThrowsAsync<NotFoundException>(
            () => new GetBoardByProjectIdQueryHandler(
                new BoardRepository(ctx2),
                new ProjectRepository(ctx2)).HandleAsync(query));
    }

    [Fact]
    public async Task HandleAsync_非メンバーが取得_ForbiddenAccessExceptionをスロー()
    {
        var (project, _, _, _, _) = await SeedAsync();
        var query = new GetBoardByProjectIdQuery(project.Id, Guid.NewGuid());

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => new GetBoardByProjectIdQueryHandler(
                new BoardRepository(ctx),
                new ProjectRepository(ctx)).HandleAsync(query));
    }
}
