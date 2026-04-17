using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Boards.Commands;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Domain.Boards;
using TaskFlow.Domain.Projects;
using TaskFlow.Infrastructure.Persistence;
using Xunit;

namespace TaskFlow.Application.Tests.Boards.Commands;

public class MoveTaskCommandTests
{
    private readonly DbContextOptions<AppDbContext> _options;

    public MoveTaskCommandTests()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private AppDbContext CreateContext() => new(_options);

    // project + 2列(columnA: 3タスク, columnB: 2タスク) をシード
    private async Task<(Project project, Column columnA, Column columnB, BoardTask[] tasksA, BoardTask[] tasksB, Guid ownerId)> SeedAsync()
    {
        var ownerId = Guid.NewGuid();
        var project = Project.Create("プロジェクト", "説明", ownerId);
        var board = Board.Create(project.Id, "ボード");
        var columnA = Column.Create(board.Id, "ToDo", 0);
        var columnB = Column.Create(board.Id, "Done", 1);

        var taskA0 = BoardTask.Create(columnA.Id, "タスクA-0", "", null, 0);
        var taskA1 = BoardTask.Create(columnA.Id, "タスクA-1", "", null, 1);
        var taskA2 = BoardTask.Create(columnA.Id, "タスクA-2", "", null, 2);
        var taskB0 = BoardTask.Create(columnB.Id, "タスクB-0", "", null, 0);
        var taskB1 = BoardTask.Create(columnB.Id, "タスクB-1", "", null, 1);

        await using var ctx = CreateContext();
        var repo = new BoardRepository(ctx);
        await new ProjectRepository(ctx).AddAsync(project);
        await repo.AddAsync(board);
        await repo.AddColumnAsync(columnA);
        await repo.AddColumnAsync(columnB);
        await repo.AddTaskAsync(taskA0);
        await repo.AddTaskAsync(taskA1);
        await repo.AddTaskAsync(taskA2);
        await repo.AddTaskAsync(taskB0);
        await repo.AddTaskAsync(taskB1);

        return (project, columnA, columnB,
            new[] { taskA0, taskA1, taskA2 },
            new[] { taskB0, taskB1 },
            ownerId);
    }

    [Fact]
    public async Task HandleAsync_同一列内で前方に移動_移動タスクのOrderが更新される()
    {
        // A2(order=2) を position 0 に移動
        var (_, columnA, _, tasksA, _, ownerId) = await SeedAsync();
        var command = new MoveTaskCommand(tasksA[2].Id, columnA.Id, 0, ownerId);

        await using var ctx = CreateContext();
        var result = await new MoveTaskCommandHandler(
            new BoardRepository(ctx),
            new ProjectRepository(ctx)).HandleAsync(command);

        Assert.Equal(columnA.Id, result.ColumnId);
        Assert.Equal(0, result.Order);
    }

    [Fact]
    public async Task HandleAsync_同一列内移動_他のタスクのOrderが振り直される()
    {
        // A0(order=0) を position 2 に移動 → A1→0, A2→1, A0→2
        var (_, columnA, _, tasksA, _, ownerId) = await SeedAsync();
        var command = new MoveTaskCommand(tasksA[0].Id, columnA.Id, 2, ownerId);

        await using var ctx = CreateContext();
        await new MoveTaskCommandHandler(
            new BoardRepository(ctx),
            new ProjectRepository(ctx)).HandleAsync(command);

        await using var verify = CreateContext();
        var verifyRepo = new BoardRepository(verify);
        var a0 = await verifyRepo.GetTaskByIdAsync(tasksA[0].Id);
        var a1 = await verifyRepo.GetTaskByIdAsync(tasksA[1].Id);
        var a2 = await verifyRepo.GetTaskByIdAsync(tasksA[2].Id);
        Assert.Equal(2, a0!.Order);
        Assert.Equal(0, a1!.Order);
        Assert.Equal(1, a2!.Order);
    }

    [Fact]
    public async Task HandleAsync_列間移動_ColumnIdとOrderが更新される()
    {
        // A2 を columnB の先頭(0)へ
        var (_, _, columnB, tasksA, _, ownerId) = await SeedAsync();
        var command = new MoveTaskCommand(tasksA[2].Id, columnB.Id, 0, ownerId);

        await using var ctx = CreateContext();
        var result = await new MoveTaskCommandHandler(
            new BoardRepository(ctx),
            new ProjectRepository(ctx)).HandleAsync(command);

        Assert.Equal(columnB.Id, result.ColumnId);
        Assert.Equal(0, result.Order);
    }

    [Fact]
    public async Task HandleAsync_列間移動_元列のタスクが詰め直される()
    {
        // A1 を columnB へ移動 → columnA には A0(0), A2(1) が残る
        var (_, _, columnB, tasksA, _, ownerId) = await SeedAsync();
        var command = new MoveTaskCommand(tasksA[1].Id, columnB.Id, 0, ownerId);

        await using var ctx = CreateContext();
        await new MoveTaskCommandHandler(
            new BoardRepository(ctx),
            new ProjectRepository(ctx)).HandleAsync(command);

        await using var verify = CreateContext();
        var verifyRepo = new BoardRepository(verify);
        var a0 = await verifyRepo.GetTaskByIdAsync(tasksA[0].Id);
        var a2 = await verifyRepo.GetTaskByIdAsync(tasksA[2].Id);
        Assert.Equal(0, a0!.Order);
        Assert.Equal(1, a2!.Order);
    }

    [Fact]
    public async Task HandleAsync_列間移動_移動先列のタスクが正しく並ぶ()
    {
        // A0 を columnB の position 1 へ → B0(0), A0(1), B1(2)
        var (_, _, columnB, tasksA, tasksB, ownerId) = await SeedAsync();
        var command = new MoveTaskCommand(tasksA[0].Id, columnB.Id, 1, ownerId);

        await using var ctx = CreateContext();
        await new MoveTaskCommandHandler(
            new BoardRepository(ctx),
            new ProjectRepository(ctx)).HandleAsync(command);

        await using var verify = CreateContext();
        var verifyRepo = new BoardRepository(verify);
        var b0 = await verifyRepo.GetTaskByIdAsync(tasksB[0].Id);
        var a0 = await verifyRepo.GetTaskByIdAsync(tasksA[0].Id);
        var b1 = await verifyRepo.GetTaskByIdAsync(tasksB[1].Id);
        Assert.Equal(0, b0!.Order);
        Assert.Equal(1, a0!.Order);
        Assert.Equal(2, b1!.Order);
    }

    [Fact]
    public async Task HandleAsync_NewOrderが列サイズを超える_末尾に移動する()
    {
        // columnA(3タスク)で newOrder=99 → 末尾(2)にクランプ
        var (_, columnA, _, tasksA, _, ownerId) = await SeedAsync();
        var command = new MoveTaskCommand(tasksA[0].Id, columnA.Id, 99, ownerId);

        await using var ctx = CreateContext();
        var result = await new MoveTaskCommandHandler(
            new BoardRepository(ctx),
            new ProjectRepository(ctx)).HandleAsync(command);

        Assert.Equal(2, result.Order);
    }

    [Fact]
    public async Task HandleAsync_存在しないタスク_NotFoundExceptionをスロー()
    {
        var command = new MoveTaskCommand(Guid.NewGuid(), Guid.NewGuid(), 0, Guid.NewGuid());

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<NotFoundException>(
            () => new MoveTaskCommandHandler(
                new BoardRepository(ctx),
                new ProjectRepository(ctx)).HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_存在しない移動先列_NotFoundExceptionをスロー()
    {
        var (_, _, _, tasksA, _, ownerId) = await SeedAsync();
        var command = new MoveTaskCommand(tasksA[0].Id, Guid.NewGuid(), 0, ownerId);

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<NotFoundException>(
            () => new MoveTaskCommandHandler(
                new BoardRepository(ctx),
                new ProjectRepository(ctx)).HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_非メンバーが移動_ForbiddenAccessExceptionをスロー()
    {
        var (_, columnA, _, tasksA, _, _) = await SeedAsync();
        var command = new MoveTaskCommand(tasksA[0].Id, columnA.Id, 1, Guid.NewGuid());

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => new MoveTaskCommandHandler(
                new BoardRepository(ctx),
                new ProjectRepository(ctx)).HandleAsync(command));
    }
}

public class MoveTaskCommandValidatorTests
{
    [Fact]
    public void Validate_正常な入力_バリデーション成功()
    {
        var command = new MoveTaskCommand(Guid.NewGuid(), Guid.NewGuid(), 0, Guid.NewGuid());
        var result = new MoveTaskCommandValidator().Validate(command);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_TargetColumnIdが空_バリデーション失敗()
    {
        var command = new MoveTaskCommand(Guid.NewGuid(), Guid.Empty, 0, Guid.NewGuid());
        var result = new MoveTaskCommandValidator().Validate(command);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_NewOrderが負数_バリデーション失敗()
    {
        var command = new MoveTaskCommand(Guid.NewGuid(), Guid.NewGuid(), -1, Guid.NewGuid());
        var result = new MoveTaskCommandValidator().Validate(command);
        Assert.False(result.IsValid);
    }
}
