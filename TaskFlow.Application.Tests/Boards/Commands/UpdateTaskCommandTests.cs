using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Boards.Commands;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Domain.Boards;
using TaskFlow.Domain.Projects;
using TaskFlow.Infrastructure.Persistence;
using Xunit;

namespace TaskFlow.Application.Tests.Boards.Commands;

public class UpdateTaskCommandTests
{
    private readonly DbContextOptions<AppDbContext> _options;

    public UpdateTaskCommandTests()
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
    public async Task HandleAsync_正常更新_タスク情報が変更される()
    {
        var (_, task, ownerId) = await SeedAsync();
        var command = new UpdateTaskCommand(task.Id, "更新タスク", "新しい説明", null, null, Priority.Medium, ownerId);

        await using var ctx = CreateContext();
        var result = await new UpdateTaskCommandHandler(
            new BoardRepository(ctx),
            new ProjectRepository(ctx)).HandleAsync(command);

        Assert.Equal("更新タスク", result.Title);
        Assert.Equal("新しい説明", result.Description);
    }

    [Fact]
    public async Task HandleAsync_担当者変更_AssigneeIdが更新される()
    {
        var (_, task, ownerId) = await SeedAsync();
        var newAssignee = Guid.NewGuid();
        var command = new UpdateTaskCommand(task.Id, "タスク1", "説明", newAssignee, null, Priority.Medium, ownerId);

        await using var ctx = CreateContext();
        var result = await new UpdateTaskCommandHandler(
            new BoardRepository(ctx),
            new ProjectRepository(ctx)).HandleAsync(command);

        Assert.Equal(newAssignee, result.AssigneeId);
    }

    [Fact]
    public async Task HandleAsync_期限と優先度を変更_更新される()
    {
        var (_, task, ownerId) = await SeedAsync();
        var dueDate = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var command = new UpdateTaskCommand(task.Id, "タスク1", "説明", null, dueDate, Priority.High, ownerId);

        await using var ctx = CreateContext();
        var result = await new UpdateTaskCommandHandler(
            new BoardRepository(ctx),
            new ProjectRepository(ctx)).HandleAsync(command);

        Assert.Equal(dueDate, result.DueDate);
        Assert.Equal(Priority.High, result.Priority);
    }

    [Fact]
    public async Task HandleAsync_存在しないタスク_NotFoundExceptionをスロー()
    {
        var command = new UpdateTaskCommand(Guid.NewGuid(), "タスク", "説明", null, null, Priority.Medium, Guid.NewGuid());

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<NotFoundException>(
            () => new UpdateTaskCommandHandler(
                new BoardRepository(ctx),
                new ProjectRepository(ctx)).HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_非メンバーが更新_ForbiddenAccessExceptionをスロー()
    {
        var (_, task, _) = await SeedAsync();
        var command = new UpdateTaskCommand(task.Id, "タスク", "説明", null, null, Priority.Medium, Guid.NewGuid());

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => new UpdateTaskCommandHandler(
                new BoardRepository(ctx),
                new ProjectRepository(ctx)).HandleAsync(command));
    }
}
