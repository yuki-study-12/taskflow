using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Projects.Commands;
using TaskFlow.Domain.Projects;
using TaskFlow.Infrastructure.Persistence;
using Xunit;

namespace TaskFlow.Application.Tests.Projects.Commands;

public class UpdateProjectCommandTests
{
    private readonly DbContextOptions<AppDbContext> _options;

    public UpdateProjectCommandTests()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private AppDbContext CreateContext() => new(_options);

    private async Task<Project> SeedProjectAsync(Guid ownerId)
    {
        var project = Project.Create("元の名前", "元の説明", ownerId);
        await using var ctx = CreateContext();
        await new ProjectRepository(ctx).AddAsync(project);
        return project;
    }

    // ── ハンドラー ──────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_Ownerが更新_更新後のProjectDtoを返す()
    {
        var ownerId = Guid.NewGuid();
        var project = await SeedProjectAsync(ownerId);
        var command = new UpdateProjectCommand(project.Id, "新しい名前", "新しい説明", ownerId);

        await using var ctx = CreateContext();
        var result = await new UpdateProjectCommandHandler(new ProjectRepository(ctx)).HandleAsync(command);

        Assert.Equal("新しい名前", result.Name);
        Assert.Equal("新しい説明", result.Description);
        Assert.Equal(MemberRole.Owner.Value, result.UserRole);
    }

    [Fact]
    public async Task HandleAsync_Ownerが更新_変更がDBに保存される()
    {
        var ownerId = Guid.NewGuid();
        var project = await SeedProjectAsync(ownerId);
        var command = new UpdateProjectCommand(project.Id, "新しい名前", "新しい説明", ownerId);

        await using (var ctx = CreateContext())
            await new UpdateProjectCommandHandler(new ProjectRepository(ctx)).HandleAsync(command);

        await using var verify = CreateContext();
        var updated = await new ProjectRepository(verify).GetByIdAsync(project.Id);
        Assert.Equal("新しい名前", updated!.Name);
        Assert.Equal("新しい説明", updated.Description);
    }

    [Fact]
    public async Task HandleAsync_メンバーが更新_メンバーのロールを返す()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var project = Project.Create("元の名前", "元の説明", ownerId);
        project.AddMember(memberId, MemberRole.Admin);

        await using (var ctx = CreateContext())
            await new ProjectRepository(ctx).AddAsync(project);

        var command = new UpdateProjectCommand(project.Id, "新しい名前", "新しい説明", memberId);

        await using var ctx2 = CreateContext();
        var result = await new UpdateProjectCommandHandler(new ProjectRepository(ctx2)).HandleAsync(command);

        Assert.Equal(MemberRole.Admin.Value, result.UserRole);
    }

    [Fact]
    public async Task HandleAsync_存在しないプロジェクト_NotFoundExceptionをスロー()
    {
        var command = new UpdateProjectCommand(Guid.NewGuid(), "名前", "説明", Guid.NewGuid());

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<NotFoundException>(
            () => new UpdateProjectCommandHandler(new ProjectRepository(ctx)).HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_非メンバーが更新_ForbiddenAccessExceptionをスロー()
    {
        var ownerId = Guid.NewGuid();
        var project = await SeedProjectAsync(ownerId);
        var command = new UpdateProjectCommand(project.Id, "新しい名前", "新しい説明", Guid.NewGuid());

        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => new UpdateProjectCommandHandler(new ProjectRepository(ctx)).HandleAsync(command));
    }

    // ── バリデーター ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("", "説明文")]
    [InlineData("プロジェクト名", "")]
    public void Validate_空文字_バリデーションエラーになる(string name, string description)
    {
        var command = new UpdateProjectCommand(Guid.NewGuid(), name, description, Guid.NewGuid());
        var validator = new UpdateProjectCommandValidator();

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }
}
