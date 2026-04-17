using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Boards.Commands;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Domain.Projects;
using TaskFlow.Infrastructure.Persistence;
using Xunit;

namespace TaskFlow.Application.Tests.Boards.Commands;

public class CreateBoardCommandTests
{
    private readonly DbContextOptions<AppDbContext> _options;

    public CreateBoardCommandTests()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private AppDbContext CreateContext() => new(_options);

    private async Task<(Project project, Guid ownerId)> SeedProjectAsync()
    {
        var ownerId = Guid.NewGuid();
        var project = Project.Create("プロジェクト", "説明", ownerId);
        await using var ctx = CreateContext();
        await new ProjectRepository(ctx).AddAsync(project);
        return (project, ownerId);
    }

    [Fact]
    public async Task HandleAsync_正常作成_ボードがDBに保存される()
    {
        var (project, ownerId) = await SeedProjectAsync();
        var command = new CreateBoardCommand(project.Id, "スプリントボード", ownerId);

        await using var ctx = CreateContext();
        var result = await new CreateBoardCommandHandler(
            new BoardRepository(ctx),
            new ProjectRepository(ctx)).HandleAsync(command);

        Assert.Equal("スプリントボード", result.Name);
        Assert.Equal(project.Id, result.ProjectId);
    }

    [Fact]
    public async Task HandleAsync_正常作成_空のColumnsが返る()
    {
        var (project, ownerId) = await SeedProjectAsync();
        var command = new CreateBoardCommand(project.Id, "ボード", ownerId);

        await using var ctx = CreateContext();
        var result = await new CreateBoardCommandHandler(
            new BoardRepository(ctx),
            new ProjectRepository(ctx)).HandleAsync(command);

        Assert.Empty(result.Columns);
    }

    [Fact]
    public async Task HandleAsync_存在しないプロジェクト_NotFoundExceptionをスロー()
    {
        var command = new CreateBoardCommand(Guid.NewGuid(), "ボード", Guid.NewGuid());

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<NotFoundException>(
            () => new CreateBoardCommandHandler(
                new BoardRepository(ctx),
                new ProjectRepository(ctx)).HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_非メンバーが作成_ForbiddenAccessExceptionをスロー()
    {
        var (project, _) = await SeedProjectAsync();
        var command = new CreateBoardCommand(project.Id, "ボード", Guid.NewGuid());

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => new CreateBoardCommandHandler(
                new BoardRepository(ctx),
                new ProjectRepository(ctx)).HandleAsync(command));
    }
}
