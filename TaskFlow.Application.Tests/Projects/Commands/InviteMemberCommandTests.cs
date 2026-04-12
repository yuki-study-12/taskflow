using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Projects.Commands;
using TaskFlow.Domain.Projects;
using TaskFlow.Infrastructure.Persistence;
using Xunit;

namespace TaskFlow.Application.Tests.Projects.Commands;

public class InviteMemberCommandTests
{
    private readonly DbContextOptions<AppDbContext> _options;

    public InviteMemberCommandTests()
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
    public async Task HandleAsync_Ownerが招待_MemberDtoを返す()
    {
        var ownerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var project = await SeedProjectAsync(ownerId);

        await using var ctx = CreateContext();
        var result = await new InviteMemberCommandHandler(new ProjectRepository(ctx))
            .HandleAsync(new InviteMemberCommand(project.Id, ownerId, targetId, "Member"));

        Assert.Equal(targetId, result.UserId);
        Assert.Equal(MemberRole.Member.Value, result.Role);
    }

    [Fact]
    public async Task HandleAsync_Adminが招待_MemberDtoを返す()
    {
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var project = await SeedProjectAsync(ownerId, p => p.AddMember(adminId, MemberRole.Admin));

        await using var ctx = CreateContext();
        var result = await new InviteMemberCommandHandler(new ProjectRepository(ctx))
            .HandleAsync(new InviteMemberCommand(project.Id, adminId, targetId, "Member"));

        Assert.Equal(targetId, result.UserId);
        Assert.Equal(MemberRole.Member.Value, result.Role);
    }

    [Fact]
    public async Task HandleAsync_Ownerが招待_DBにメンバーが追加される()
    {
        var ownerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var project = await SeedProjectAsync(ownerId);

        await using (var ctx = CreateContext())
            await new InviteMemberCommandHandler(new ProjectRepository(ctx))
                .HandleAsync(new InviteMemberCommand(project.Id, ownerId, targetId, "Admin"));

        await using var verify = CreateContext();
        var saved = await verify.Projects.Include(p => p.Members).FirstAsync(p => p.Id == project.Id);
        Assert.Equal(2, saved.Members.Count);
        Assert.Contains(saved.Members, m => m.UserId == targetId && m.Role == MemberRole.Admin);
    }

    [Fact]
    public async Task HandleAsync_Memberロールが招待_ForbiddenAccessExceptionをスロー()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var project = await SeedProjectAsync(ownerId, p => p.AddMember(memberId, MemberRole.Member));

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => new InviteMemberCommandHandler(new ProjectRepository(ctx))
                .HandleAsync(new InviteMemberCommand(project.Id, memberId, targetId, "Member")));
    }

    [Fact]
    public async Task HandleAsync_非メンバーが招待_ForbiddenAccessExceptionをスロー()
    {
        var ownerId = Guid.NewGuid();
        var project = await SeedProjectAsync(ownerId);

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => new InviteMemberCommandHandler(new ProjectRepository(ctx))
                .HandleAsync(new InviteMemberCommand(project.Id, Guid.NewGuid(), Guid.NewGuid(), "Member")));
    }

    [Fact]
    public async Task HandleAsync_存在しないプロジェクト_NotFoundExceptionをスロー()
    {
        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<NotFoundException>(
            () => new InviteMemberCommandHandler(new ProjectRepository(ctx))
                .HandleAsync(new InviteMemberCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Member")));
    }

    [Fact]
    public async Task HandleAsync_既存メンバーを招待_InvalidOperationExceptionをスロー()
    {
        var ownerId = Guid.NewGuid();
        var project = await SeedProjectAsync(ownerId);

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => new InviteMemberCommandHandler(new ProjectRepository(ctx))
                .HandleAsync(new InviteMemberCommand(project.Id, ownerId, ownerId, "Member")));
    }

    // ── バリデーター ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Owner")]
    [InlineData("invalid")]
    [InlineData("")]
    public void Validate_無効なRole_バリデーションエラーになる(string role)
    {
        var command = new InviteMemberCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), role);
        var validator = new InviteMemberCommandValidator();

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.Role));
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Member")]
    public void Validate_有効なRole_バリデーション成功(string role)
    {
        var command = new InviteMemberCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), role);
        var validator = new InviteMemberCommandValidator();

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
