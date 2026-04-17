using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Boards.Commands;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Domain.Boards;
using TaskFlow.Domain.Projects;
using TaskFlow.Infrastructure.Persistence;
using Xunit;

namespace TaskFlow.Application.Tests.Boards.Commands;

public class UpdateColumnCommandTests
{
    private readonly DbContextOptions<AppDbContext> _options;

    public UpdateColumnCommandTests()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private AppDbContext CreateContext() => new(_options);

    private async Task<(Project project, Board board, Column column, Guid ownerId)> SeedAsync()
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
        return (project, board, column, ownerId);
    }

    [Fact]
    public async Task HandleAsync_正常更新_列名が変更される()
    {
        var (_, _, column, ownerId) = await SeedAsync();
        var command = new UpdateColumnCommand(column.Id, "進行中", ownerId);

        await using var ctx = CreateContext();
        var result = await new UpdateColumnCommandHandler(
            new BoardRepository(ctx),
            new ProjectRepository(ctx)).HandleAsync(command);

        Assert.Equal("進行中", result.Name);
    }

    [Fact]
    public async Task HandleAsync_存在しない列_NotFoundExceptionをスロー()
    {
        var command = new UpdateColumnCommand(Guid.NewGuid(), "新名前", Guid.NewGuid());

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<NotFoundException>(
            () => new UpdateColumnCommandHandler(
                new BoardRepository(ctx),
                new ProjectRepository(ctx)).HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_非メンバーが更新_ForbiddenAccessExceptionをスロー()
    {
        var (_, _, column, _) = await SeedAsync();
        var command = new UpdateColumnCommand(column.Id, "新名前", Guid.NewGuid());

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => new UpdateColumnCommandHandler(
                new BoardRepository(ctx),
                new ProjectRepository(ctx)).HandleAsync(command));
    }
}
