using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Projects.Queries;
using TaskFlow.Domain.Projects;
using TaskFlow.Infrastructure.Persistence;
using Xunit;

namespace TaskFlow.Application.Tests.Projects.Queries;

public class GetProjectsQueryTests
{
    private readonly DbContextOptions<AppDbContext> _options;

    public GetProjectsQueryTests()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private AppDbContext CreateContext() => new(_options);

    [Fact]
    public async Task HandleAsync_参加プロジェクトあり_正しいリストを返す()
    {
        var userId = Guid.NewGuid();
        var myProject = Project.Create("自分のプロジェクト", "説明", userId);
        var otherProject = Project.Create("他人のプロジェクト", "説明", Guid.NewGuid());

        await using (var ctx = CreateContext())
        {
            var repo = new ProjectRepository(ctx);
            await repo.AddAsync(myProject);
            await repo.AddAsync(otherProject);
        }

        await using var ctx2 = CreateContext();
        var result = await new GetProjectsQueryHandler(new ProjectRepository(ctx2))
            .HandleAsync(new GetProjectsQuery(userId));

        Assert.Single(result);
        Assert.Equal(myProject.Id, result[0].Id);
        Assert.Equal("自分のプロジェクト", result[0].Name);
    }

    [Fact]
    public async Task HandleAsync_参加プロジェクトなし_空リストを返す()
    {
        await using var ctx = CreateContext();
        var result = await new GetProjectsQueryHandler(new ProjectRepository(ctx))
            .HandleAsync(new GetProjectsQuery(Guid.NewGuid()));

        Assert.Empty(result);
    }

    [Fact]
    public async Task HandleAsync_Owner_UserRoleがOwnerになる()
    {
        var userId = Guid.NewGuid();
        var project = Project.Create("プロジェクト", "説明", userId);

        await using (var ctx = CreateContext())
            await new ProjectRepository(ctx).AddAsync(project);

        await using var ctx2 = CreateContext();
        var result = await new GetProjectsQueryHandler(new ProjectRepository(ctx2))
            .HandleAsync(new GetProjectsQuery(userId));

        Assert.Equal(MemberRole.Owner.Value, result[0].UserRole);
    }

    [Fact]
    public async Task HandleAsync_Member_UserRoleがMemberになる()
    {
        var memberId = Guid.NewGuid();
        var project = Project.Create("プロジェクト", "説明", Guid.NewGuid());
        project.AddMember(memberId, MemberRole.Member);

        await using (var ctx = CreateContext())
            await new ProjectRepository(ctx).AddAsync(project);

        await using var ctx2 = CreateContext();
        var result = await new GetProjectsQueryHandler(new ProjectRepository(ctx2))
            .HandleAsync(new GetProjectsQuery(memberId));

        Assert.Equal(MemberRole.Member.Value, result[0].UserRole);
    }

    [Fact]
    public async Task HandleAsync_複数プロジェクト_すべて返す()
    {
        var userId = Guid.NewGuid();
        var project1 = Project.Create("プロジェクト1", "説明", userId);
        var project2 = Project.Create("プロジェクト2", "説明", userId);

        await using (var ctx = CreateContext())
        {
            var repo = new ProjectRepository(ctx);
            await repo.AddAsync(project1);
            await repo.AddAsync(project2);
        }

        await using var ctx2 = CreateContext();
        var result = await new GetProjectsQueryHandler(new ProjectRepository(ctx2))
            .HandleAsync(new GetProjectsQuery(userId));

        Assert.Equal(2, result.Count);
    }
}
