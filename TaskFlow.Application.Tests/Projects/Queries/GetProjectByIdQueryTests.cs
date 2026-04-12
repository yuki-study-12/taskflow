using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Projects.Queries;
using TaskFlow.Domain.Projects;
using TaskFlow.Infrastructure.Persistence;
using Xunit;

namespace TaskFlow.Application.Tests.Projects.Queries;

public class GetProjectByIdQueryTests
{
    private readonly DbContextOptions<AppDbContext> _options;

    public GetProjectByIdQueryTests()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private AppDbContext CreateContext() => new(_options);

    [Fact]
    public async Task HandleAsync_メンバーが取得_正しいProjectDtoを返す()
    {
        var ownerId = Guid.NewGuid();
        var project = Project.Create("プロジェクト", "説明", ownerId);

        await using (var ctx = CreateContext())
            await new ProjectRepository(ctx).AddAsync(project);

        await using var ctx2 = CreateContext();
        var result = await new GetProjectByIdQueryHandler(new ProjectRepository(ctx2))
            .HandleAsync(new GetProjectByIdQuery(project.Id, ownerId));

        Assert.Equal(project.Id, result.Id);
        Assert.Equal("プロジェクト", result.Name);
        Assert.Equal("説明", result.Description);
        Assert.Equal(MemberRole.Owner.Value, result.UserRole);
    }

    [Fact]
    public async Task HandleAsync_存在しないプロジェクト_NotFoundExceptionをスロー()
    {
        await using var ctx = CreateContext();
        await Assert.ThrowsAsync<NotFoundException>(
            () => new GetProjectByIdQueryHandler(new ProjectRepository(ctx))
                .HandleAsync(new GetProjectByIdQuery(Guid.NewGuid(), Guid.NewGuid())));
    }

    [Fact]
    public async Task HandleAsync_非メンバーが取得_ForbiddenAccessExceptionをスロー()
    {
        var ownerId = Guid.NewGuid();
        var project = Project.Create("プロジェクト", "説明", ownerId);

        await using (var ctx = CreateContext())
            await new ProjectRepository(ctx).AddAsync(project);

        await using var ctx2 = CreateContext();
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => new GetProjectByIdQueryHandler(new ProjectRepository(ctx2))
                .HandleAsync(new GetProjectByIdQuery(project.Id, Guid.NewGuid())));
    }

    [Fact]
    public async Task HandleAsync_Adminメンバーが取得_UserRoleがAdminになる()
    {
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var project = Project.Create("プロジェクト", "説明", ownerId);
        project.AddMember(adminId, MemberRole.Admin);

        await using (var ctx = CreateContext())
            await new ProjectRepository(ctx).AddAsync(project);

        await using var ctx2 = CreateContext();
        var result = await new GetProjectByIdQueryHandler(new ProjectRepository(ctx2))
            .HandleAsync(new GetProjectByIdQuery(project.Id, adminId));

        Assert.Equal(MemberRole.Admin.Value, result.UserRole);
    }
}
