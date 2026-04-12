using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Projects.Commands;
using TaskFlow.Domain.Projects;
using TaskFlow.Infrastructure.Persistence;
using Xunit;

namespace TaskFlow.Application.Tests.Projects.Commands;

public class DeleteProjectCommandTests
{
    private readonly DbContextOptions<AppDbContext> _options;

    public DeleteProjectCommandTests()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private AppDbContext CreateContext() => new(_options);

    private async Task<Project> SeedProjectAsync(Guid ownerId)
    {
        var project = Project.Create("プロジェクト", "説明", ownerId);
        await using var ctx = CreateContext();
        await new ProjectRepository(ctx).AddAsync(project);
        return project;
    }

    // ── ハンドラー ──────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_Ownerが削除_プロジェクトがDBから消える()
    {
        var ownerId = Guid.NewGuid();
        var project = await SeedProjectAsync(ownerId);
        var command = new DeleteProjectCommand(project.Id, ownerId);

        await using (var ctx = CreateContext())
            await new DeleteProjectCommandHandler(new ProjectRepository(ctx)).HandleAsync(command);

        await using var verify = CreateContext();
        var result = await new ProjectRepository(verify).GetByIdAsync(project.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_Ownerが削除_trueを返す()
    {
        var ownerId = Guid.NewGuid();
        var project = await SeedProjectAsync(ownerId);
        var command = new DeleteProjectCommand(project.Id, ownerId);

        await using var ctx = CreateContext();
        var result = await new DeleteProjectCommandHandler(new ProjectRepository(ctx)).HandleAsync(command);

        Assert.True(result);
    }

    [Fact]
    public async Task HandleAsync_非メンバーが削除_ForbiddenAccessExceptionをスロー()
    {
        var ownerId = Guid.NewGuid();
        var project = await SeedProjectAsync(ownerId);
        var command = new DeleteProjectCommand(project.Id, Guid.NewGuid());

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => new DeleteProjectCommandHandler(new ProjectRepository(ctx)).HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_Memberロールが削除_ForbiddenAccessExceptionをスロー()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var project = Project.Create("プロジェクト", "説明", ownerId);
        project.AddMember(memberId, MemberRole.Member);

        await using (var ctx = CreateContext())
            await new ProjectRepository(ctx).AddAsync(project);

        var command = new DeleteProjectCommand(project.Id, memberId);

        await using var ctx2 = CreateContext();
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => new DeleteProjectCommandHandler(new ProjectRepository(ctx2)).HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_存在しないプロジェクト_NotFoundExceptionをスロー()
    {
        var command = new DeleteProjectCommand(Guid.NewGuid(), Guid.NewGuid());

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<NotFoundException>(
            () => new DeleteProjectCommandHandler(new ProjectRepository(ctx)).HandleAsync(command));
    }
}
