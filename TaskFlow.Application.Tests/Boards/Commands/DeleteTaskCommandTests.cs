using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Boards.Commands;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Domain.Boards;
using TaskFlow.Domain.Projects;
using TaskFlow.Infrastructure.Persistence;
using Xunit;

namespace TaskFlow.Application.Tests.Boards.Commands;

public class DeleteTaskCommandTests
{
    private readonly DbContextOptions<AppDbContext> _options;

    public DeleteTaskCommandTests()
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
    public async Task HandleAsync_正常削除_タスクがDBから消える()
    {
        var (_, task, ownerId) = await SeedAsync();
        var command = new DeleteTaskCommand(task.Id, ownerId);

        await using var ctx = CreateContext();
        await new DeleteTaskCommandHandler(
            new BoardRepository(ctx),
            new ProjectRepository(ctx)).HandleAsync(command);

        await using var verify = CreateContext();
        var result = await new BoardRepository(verify).GetTaskByIdAsync(task.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_正常削除_trueを返す()
    {
        var (_, task, ownerId) = await SeedAsync();
        var command = new DeleteTaskCommand(task.Id, ownerId);

        await using var ctx = CreateContext();
        var result = await new DeleteTaskCommandHandler(
            new BoardRepository(ctx),
            new ProjectRepository(ctx)).HandleAsync(command);

        Assert.True(result);
    }

    [Fact]
    public async Task HandleAsync_存在しないタスク_NotFoundExceptionをスロー()
    {
        var command = new DeleteTaskCommand(Guid.NewGuid(), Guid.NewGuid());

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<NotFoundException>(
            () => new DeleteTaskCommandHandler(
                new BoardRepository(ctx),
                new ProjectRepository(ctx)).HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_非メンバーが削除_ForbiddenAccessExceptionをスロー()
    {
        var (_, task, _) = await SeedAsync();
        var command = new DeleteTaskCommand(task.Id, Guid.NewGuid());

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => new DeleteTaskCommandHandler(
                new BoardRepository(ctx),
                new ProjectRepository(ctx)).HandleAsync(command));
    }
}
