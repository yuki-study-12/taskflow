using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Projects.Commands;
using TaskFlow.Application.Tests.TestDoubles;
using TaskFlow.Domain.Projects;
using TaskFlow.Infrastructure.Persistence;
using Xunit;

namespace TaskFlow.Application.Tests.Projects.Commands;

public class UpdateMemberRoleCommandTests
{
    private readonly DbContextOptions<AppDbContext> _options;

    public UpdateMemberRoleCommandTests()
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
    public async Task HandleAsync_Ownerが権限変更_MemberDtoを返す()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var project = await SeedProjectAsync(ownerId, p => p.AddMember(memberId, MemberRole.Member));

        await using var ctx = CreateContext();
        var result = await new UpdateMemberRoleCommandHandler(new ProjectRepository(ctx), new FakeIdentityService())
            .HandleAsync(new UpdateMemberRoleCommand(project.Id, ownerId, memberId, "Admin"));

        Assert.Equal(memberId, result.UserId);
        Assert.Equal(MemberRole.Admin.Value, result.Role);
    }

    [Fact]
    public async Task HandleAsync_Adminが権限変更_MemberDtoを返す()
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
        var result = await new UpdateMemberRoleCommandHandler(new ProjectRepository(ctx), new FakeIdentityService())
            .HandleAsync(new UpdateMemberRoleCommand(project.Id, adminId, memberId, "Admin"));

        Assert.Equal(memberId, result.UserId);
        Assert.Equal(MemberRole.Admin.Value, result.Role);
    }

    [Fact]
    public async Task HandleAsync_Ownerが権限変更_DBに反映される()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var project = await SeedProjectAsync(ownerId, p => p.AddMember(memberId, MemberRole.Member));

        await using (var ctx = CreateContext())
            await new UpdateMemberRoleCommandHandler(new ProjectRepository(ctx), new FakeIdentityService())
                .HandleAsync(new UpdateMemberRoleCommand(project.Id, ownerId, memberId, "Admin"));

        await using var verify = CreateContext();
        var saved = await verify.Projects.Include(p => p.Members).FirstAsync(p => p.Id == project.Id);
        var updated = saved.Members.First(m => m.UserId == memberId);
        Assert.Equal(MemberRole.Admin, updated.Role);
    }

    [Fact]
    public async Task HandleAsync_Memberロールが権限変更_ForbiddenAccessExceptionをスロー()
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
            () => new UpdateMemberRoleCommandHandler(new ProjectRepository(ctx), new FakeIdentityService())
                .HandleAsync(new UpdateMemberRoleCommand(project.Id, memberId, targetId, "Admin")));
    }

    [Fact]
    public async Task HandleAsync_非メンバーが権限変更_ForbiddenAccessExceptionをスロー()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var project = await SeedProjectAsync(ownerId, p => p.AddMember(memberId, MemberRole.Member));

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => new UpdateMemberRoleCommandHandler(new ProjectRepository(ctx), new FakeIdentityService())
                .HandleAsync(new UpdateMemberRoleCommand(project.Id, Guid.NewGuid(), memberId, "Admin")));
    }

    [Fact]
    public async Task HandleAsync_OwnerのRoleを変更_InvalidOperationExceptionをスロー()
    {
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var project = await SeedProjectAsync(ownerId, p => p.AddMember(adminId, MemberRole.Admin));

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => new UpdateMemberRoleCommandHandler(new ProjectRepository(ctx), new FakeIdentityService())
                .HandleAsync(new UpdateMemberRoleCommand(project.Id, adminId, ownerId, "Member")));
    }

    [Fact]
    public async Task HandleAsync_存在しないプロジェクト_NotFoundExceptionをスロー()
    {
        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<NotFoundException>(
            () => new UpdateMemberRoleCommandHandler(new ProjectRepository(ctx), new FakeIdentityService())
                .HandleAsync(new UpdateMemberRoleCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Member")));
    }

    // ── バリデーター ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Owner")]
    [InlineData("invalid")]
    [InlineData("")]
    public void Validate_無効なRole_バリデーションエラーになる(string role)
    {
        var command = new UpdateMemberRoleCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), role);
        var validator = new UpdateMemberRoleCommandValidator();

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.Role));
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Member")]
    public void Validate_有効なRole_バリデーション成功(string role)
    {
        var command = new UpdateMemberRoleCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), role);
        var validator = new UpdateMemberRoleCommandValidator();

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
