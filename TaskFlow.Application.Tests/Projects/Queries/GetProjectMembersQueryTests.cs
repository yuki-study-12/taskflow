using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Projects.Queries;
using TaskFlow.Domain.Projects;
using TaskFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace TaskFlow.Application.Tests.Projects.Queries;

public class GetProjectMembersQueryTests
{
    private readonly DbContextOptions<AppDbContext> _options;

    public GetProjectMembersQueryTests()
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
    public async Task HandleAsync_Ownerが取得_全メンバー一覧を返す()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var project = await SeedProjectAsync(ownerId, p =>
        {
            p.AddMember(adminId, MemberRole.Admin);
            p.AddMember(memberId, MemberRole.Member);
        });

        await using var ctx = CreateContext();
        var result = await new GetProjectMembersQueryHandler(new ProjectRepository(ctx))
            .HandleAsync(new GetProjectMembersQuery(project.Id, ownerId));

        Assert.Equal(3, result.Count);
        Assert.Contains(result, m => m.UserId == ownerId && m.Role == MemberRole.Owner.Value);
        Assert.Contains(result, m => m.UserId == adminId && m.Role == MemberRole.Admin.Value);
        Assert.Contains(result, m => m.UserId == memberId && m.Role == MemberRole.Member.Value);
    }

    [Fact]
    public async Task HandleAsync_Memberが取得_全メンバー一覧を返す()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var project = await SeedProjectAsync(ownerId, p => p.AddMember(memberId, MemberRole.Member));

        await using var ctx = CreateContext();
        var result = await new GetProjectMembersQueryHandler(new ProjectRepository(ctx))
            .HandleAsync(new GetProjectMembersQuery(project.Id, memberId));

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task HandleAsync_非メンバーが取得_ForbiddenAccessExceptionをスロー()
    {
        var ownerId = Guid.NewGuid();
        var project = await SeedProjectAsync(ownerId);

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => new GetProjectMembersQueryHandler(new ProjectRepository(ctx))
                .HandleAsync(new GetProjectMembersQuery(project.Id, Guid.NewGuid())));
    }

    [Fact]
    public async Task HandleAsync_存在しないプロジェクト_NotFoundExceptionをスロー()
    {
        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<NotFoundException>(
            () => new GetProjectMembersQueryHandler(new ProjectRepository(ctx))
                .HandleAsync(new GetProjectMembersQuery(Guid.NewGuid(), Guid.NewGuid())));
    }

    [Fact]
    public async Task HandleAsync_メンバーが1人_Owner1件を返す()
    {
        var ownerId = Guid.NewGuid();
        var project = await SeedProjectAsync(ownerId);

        await using var ctx = CreateContext();
        var result = await new GetProjectMembersQueryHandler(new ProjectRepository(ctx))
            .HandleAsync(new GetProjectMembersQuery(project.Id, ownerId));

        Assert.Single(result);
        Assert.Equal(MemberRole.Owner.Value, result[0].Role);
    }
}
