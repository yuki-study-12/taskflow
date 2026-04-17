using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Boards.Commands;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Domain.Boards;
using TaskFlow.Domain.Projects;
using TaskFlow.Infrastructure.Persistence;
using Xunit;

namespace TaskFlow.Application.Tests.Boards.Commands;

public class CreateTaskCommandTests
{
    private readonly DbContextOptions<AppDbContext> _options;

    public CreateTaskCommandTests()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private AppDbContext CreateContext() => new(_options);

    private async Task<(Project project, Column column, Guid ownerId)> SeedAsync()
    {
        var ownerId = Guid.NewGuid();
        var project = Project.Create("プロジェクト", "説明", ownerId);
        var board = Board.Create(project.Id, "ボード");
        var column = Column.Create(board.Id, "ToDo", 0);

        await using var ctx = CreateContext();
        var repo = new BoardRepository(ctx);
        await new ProjectRepository(ctx).AddAsync(project);
        await repo.AddAsync(board);
        await repo.AddColumnAsync(column);
        return (project, column, ownerId);
    }

    [Fact]
    public async Task HandleAsync_正常作成_タスクがDBに保存される()
    {
        var (_, column, ownerId) = await SeedAsync();
        var command = new CreateTaskCommand(column.Id, "タスク1", "説明", null, ownerId);

        await using var ctx = CreateContext();
        var result = await new CreateTaskCommandHandler(
            new BoardRepository(ctx),
            new ProjectRepository(ctx)).HandleAsync(command);

        Assert.Equal("タスク1", result.Title);
        Assert.Equal(column.Id, result.ColumnId);
        Assert.Equal(0, result.Order);
    }

    [Fact]
    public async Task HandleAsync_担当者あり_AssigneeIdが保存される()
    {
        var (_, column, ownerId) = await SeedAsync();
        var assigneeId = Guid.NewGuid();
        var command = new CreateTaskCommand(column.Id, "タスク", "説明", assigneeId, ownerId);

        await using var ctx = CreateContext();
        var result = await new CreateTaskCommandHandler(
            new BoardRepository(ctx),
            new ProjectRepository(ctx)).HandleAsync(command);

        Assert.Equal(assigneeId, result.AssigneeId);
    }

    [Fact]
    public async Task HandleAsync_存在しない列_NotFoundExceptionをスロー()
    {
        var command = new CreateTaskCommand(Guid.NewGuid(), "タスク", "説明", null, Guid.NewGuid());

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<NotFoundException>(
            () => new CreateTaskCommandHandler(
                new BoardRepository(ctx),
                new ProjectRepository(ctx)).HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_非メンバーが作成_ForbiddenAccessExceptionをスロー()
    {
        var (_, column, _) = await SeedAsync();
        var command = new CreateTaskCommand(column.Id, "タスク", "説明", null, Guid.NewGuid());

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => new CreateTaskCommandHandler(
                new BoardRepository(ctx),
                new ProjectRepository(ctx)).HandleAsync(command));
    }
}
