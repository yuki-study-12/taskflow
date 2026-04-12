using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Projects.Commands;
using TaskFlow.Domain.Projects;
using TaskFlow.Infrastructure.Persistence;
using Xunit;

namespace TaskFlow.Application.Tests.Projects.Commands;

public class RemoveMemberCommandTests
{
    private readonly DbContextOptions<AppDbContext> _options;

    public RemoveMemberCommandTests()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private AppDbContext CreateContext() => new(_options);

    private async Task<Project> SeedProjectAsync(Guid ownerId, Action<Project>? configure = null)
    {
        var project = Project.Create("プロジェクト", "説明", ownerId);
        configure?.Invoke(project);
        await using var ctx = CreateContext();
        await new ProjectRepository(ctx).AddAsync(project);
        return project;
    }

    // ── ハンドラー ──────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_Ownerが除名_trueを返す()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var project = await SeedProjectAsync(ownerId, p => p.AddMember(memberId, MemberRole.Member));

        await using var ctx = CreateContext();
        var result = await new RemoveMemberCommandHandler(new ProjectRepository(ctx))
            .HandleAsync(new RemoveMemberCommand(project.Id, ownerId, memberId));

        Assert.True(result);
    }

    [Fact]
    public async Task HandleAsync_Adminが除名_trueを返す()
    {
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var project = await SeedProjectAsync(ownerId, p =>
        {
            p.AddMember(adminId, MemberRole.Admin);
            p.AddMember(memberId, MemberRole.Member);
        });

        await using var ctx = CreateContext();
        var result = await new RemoveMemberCommandHandler(new ProjectRepository(ctx))
            .HandleAsync(new RemoveMemberCommand(project.Id, adminId, memberId));

        Assert.True(result);
    }

    [Fact]
    public async Task HandleAsync_Ownerが除名_DBからメンバーが消える()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var project = await SeedProjectAsync(ownerId, p => p.AddMember(memberId, MemberRole.Member));

        await using (var ctx = CreateContext())
            await new RemoveMemberCommandHandler(new ProjectRepository(ctx))
                .HandleAsync(new RemoveMemberCommand(project.Id, ownerId, memberId));

        await using var verify = CreateContext();
        var saved = await verify.Projects.Include(p => p.Members).FirstAsync(p => p.Id == project.Id);
        Assert.DoesNotContain(saved.Members, m => m.UserId == memberId);
    }

    [Fact]
    public async Task HandleAsync_Memberロールが除名_ForbiddenAccessExceptionをスロー()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var project = await SeedProjectAsync(ownerId, p =>
        {
            p.AddMember(memberId, MemberRole.Member);
            p.AddMember(targetId, MemberRole.Member);
        });

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => new RemoveMemberCommandHandler(new ProjectRepository(ctx))
                .HandleAsync(new RemoveMemberCommand(project.Id, memberId, targetId)));
    }

    [Fact]
    public async Task HandleAsync_非メンバーが除名_ForbiddenAccessExceptionをスロー()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var project = await SeedProjectAsync(ownerId, p => p.AddMember(memberId, MemberRole.Member));

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => new RemoveMemberCommandHandler(new ProjectRepository(ctx))
                .HandleAsync(new RemoveMemberCommand(project.Id, Guid.NewGuid(), memberId)));
    }

    [Fact]
    public async Task HandleAsync_Ownerを除名_InvalidOperationExceptionをスロー()
    {
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var project = await SeedProjectAsync(ownerId, p => p.AddMember(adminId, MemberRole.Admin));

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => new RemoveMemberCommandHandler(new ProjectRepository(ctx))
                .HandleAsync(new RemoveMemberCommand(project.Id, adminId, ownerId)));
    }

    [Fact]
    public async Task HandleAsync_存在しないプロジェクト_NotFoundExceptionをスロー()
    {
        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<NotFoundException>(
            () => new RemoveMemberCommandHandler(new ProjectRepository(ctx))
                .HandleAsync(new RemoveMemberCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid())));
    }
}
