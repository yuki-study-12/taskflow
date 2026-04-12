using Microsoft.EntityFrameworkCore;
using Xunit;
using TaskFlow.Domain.Projects;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Tests.Persistence;

public class ProjectRepositoryTests
{
    private readonly DbContextOptions<AppDbContext> _options;

    public ProjectRepositoryTests()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private AppDbContext CreateContext() => new(_options);

    // ── AddAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_プロジェクトが保存される()
    {
        var project = Project.Create("Test Project", "説明", Guid.NewGuid());

        await using (var ctx = CreateContext())
            await new ProjectRepository(ctx).AddAsync(project);

        await using var verify = CreateContext();
        var saved = await verify.Projects.FindAsync(project.Id);
        Assert.NotNull(saved);
        Assert.Equal("Test Project", saved.Name);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_存在するIDでプロジェクトとメンバーが取得できる()
    {
        var ownerId = Guid.NewGuid();
        var project = Project.Create("Test Project", "説明", ownerId);

        await using (var ctx = CreateContext())
            await new ProjectRepository(ctx).AddAsync(project);

        await using var ctx2 = CreateContext();
        var result = await new ProjectRepository(ctx2).GetByIdAsync(project.Id);

        Assert.NotNull(result);
        Assert.Equal(project.Id, result.Id);
        Assert.Single(result.Members);
        Assert.Equal(ownerId, result.Members[0].UserId);
    }

    [Fact]
    public async Task GetByIdAsync_存在しないIDでnullが返る()
    {
        await using var ctx = CreateContext();
        var result = await new ProjectRepository(ctx).GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    // ── GetByUserIdAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetByUserIdAsync_参加しているプロジェクトのみ返る()
    {
        var userId = Guid.NewGuid();
        var myProject = Project.Create("My Project", "説明", userId);
        var otherProject = Project.Create("Other Project", "説明", Guid.NewGuid());

        await using (var ctx = CreateContext())
        {
            var repo = new ProjectRepository(ctx);
            await repo.AddAsync(myProject);
            await repo.AddAsync(otherProject);
        }

        await using var ctx2 = CreateContext();
        var result = await new ProjectRepository(ctx2).GetByUserIdAsync(userId);

        Assert.Single(result);
        Assert.Equal(myProject.Id, result[0].Id);
    }

    [Fact]
    public async Task GetByUserIdAsync_メンバーとして招待されたプロジェクトも返る()
    {
        var memberId = Guid.NewGuid();
        var project = Project.Create("Project", "説明", Guid.NewGuid());
        project.AddMember(memberId, MemberRole.Member);

        await using (var ctx = CreateContext())
            await new ProjectRepository(ctx).AddAsync(project);

        await using var ctx2 = CreateContext();
        var result = await new ProjectRepository(ctx2).GetByUserIdAsync(memberId);

        Assert.Single(result);
        Assert.Equal(project.Id, result[0].Id);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_メンバー追加後の変更が保存される()
    {
        var project = Project.Create("Project", "説明", Guid.NewGuid());

        await using (var ctx = CreateContext())
            await new ProjectRepository(ctx).AddAsync(project);

        // 別コンテキストでロード→変更→保存（実際のユースケースと同じ流れ）
        await using (var ctx = CreateContext())
        {
            var repo = new ProjectRepository(ctx);
            var loaded = await repo.GetByIdAsync(project.Id);
            loaded!.AddMember(Guid.NewGuid(), MemberRole.Member);
            await repo.UpdateAsync(loaded);
        }

        await using var verify = CreateContext();
        var updated = await new ProjectRepository(verify).GetByIdAsync(project.Id);
        Assert.NotNull(updated);
        Assert.Equal(2, updated.Members.Count);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_プロジェクトが削除される()
    {
        var project = Project.Create("Project", "説明", Guid.NewGuid());

        await using (var ctx = CreateContext())
            await new ProjectRepository(ctx).AddAsync(project);

        await using (var ctx = CreateContext())
            await new ProjectRepository(ctx).DeleteAsync(project.Id);

        await using var verify = CreateContext();
        var result = await new ProjectRepository(verify).GetByIdAsync(project.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_存在しないIDで例外が発生しない()
    {
        await using var ctx = CreateContext();
        var exception = await Record.ExceptionAsync(
            () => new ProjectRepository(ctx).DeleteAsync(Guid.NewGuid()));

        Assert.Null(exception);
    }
}
