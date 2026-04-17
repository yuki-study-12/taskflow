using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Boards.Commands;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Domain.Boards;
using TaskFlow.Domain.Projects;
using TaskFlow.Infrastructure.Persistence;
using Xunit;

namespace TaskFlow.Application.Tests.Boards.Commands;

public class CreateColumnCommandTests
{
    private readonly DbContextOptions<AppDbContext> _options;

    public CreateColumnCommandTests()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private AppDbContext CreateContext() => new(_options);

    private async Task<(Project project, Board board, Guid ownerId)> SeedAsync()
    {
        var ownerId = Guid.NewGuid();
        var project = Project.Create("プロジェクト", "説明", ownerId);
        var board = Board.Create(project.Id, "ボード");

        await using var ctx = CreateContext();
        await new ProjectRepository(ctx).AddAsync(project);
        await new BoardRepository(ctx).AddAsync(board);
        return (project, board, ownerId);
    }

    [Fact]
    public async Task HandleAsync_正常作成_列がDBに保存される()
    {
        var (_, board, ownerId) = await SeedAsync();
        var command = new CreateColumnCommand(board.Id, "ToDo", ownerId);

        await using var ctx = CreateContext();
        var result = await new CreateColumnCommandHandler(
            new BoardRepository(ctx),
            new ProjectRepository(ctx)).HandleAsync(command);

        Assert.Equal("ToDo", result.Name);
        Assert.Equal(board.Id, result.BoardId);
        Assert.Equal(0, result.Order);
    }

    [Fact]
    public async Task HandleAsync_2列目作成_Orderが1になる()
    {
        var (_, board, ownerId) = await SeedAsync();

        await using var ctx1 = CreateContext();
        await new CreateColumnCommandHandler(
            new BoardRepository(ctx1),
            new ProjectRepository(ctx1)).HandleAsync(new CreateColumnCommand(board.Id, "ToDo", ownerId));

        await using var ctx2 = CreateContext();
        var result = await new CreateColumnCommandHandler(
            new BoardRepository(ctx2),
            new ProjectRepository(ctx2)).HandleAsync(new CreateColumnCommand(board.Id, "In Progress", ownerId));

        Assert.Equal(1, result.Order);
    }

    [Fact]
    public async Task HandleAsync_存在しないボード_NotFoundExceptionをスロー()
    {
        var command = new CreateColumnCommand(Guid.NewGuid(), "ToDo", Guid.NewGuid());

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<NotFoundException>(
            () => new CreateColumnCommandHandler(
                new BoardRepository(ctx),
                new ProjectRepository(ctx)).HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_非メンバーが作成_ForbiddenAccessExceptionをスロー()
    {
        var (_, board, _) = await SeedAsync();
        var command = new CreateColumnCommand(board.Id, "ToDo", Guid.NewGuid());

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => new CreateColumnCommandHandler(
                new BoardRepository(ctx),
                new ProjectRepository(ctx)).HandleAsync(command));
    }
}
