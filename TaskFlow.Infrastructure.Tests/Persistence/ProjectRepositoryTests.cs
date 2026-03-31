using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;
using TaskFlow.Domain.Projects;
using TaskFlow.Infrastructure.Identity;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Tests.Persistence;

public class ProjectRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ProjectRepository _repository;

    public ProjectRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new ProjectRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    // ── AddAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_プロジェクトが保存される()
    {
        var ownerId = Guid.NewGuid();
        var project = Project.Create("Test Project", "説明", ownerId);

        await _repository.AddAsync(project);

        var saved = await _context.Projects.FindAsync(project.Id);
        Assert.NotNull(saved);
        Assert.Equal("Test Project", saved.Name);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_存在するIDでプロジェクトとメンバーが取得できる()
    {
        var ownerId = Guid.NewGuid();
        var project = Project.Create("Test Project", "説明", ownerId);
        await _repository.AddAsync(project);

        var result = await _repository.GetByIdAsync(project.Id);

        Assert.NotNull(result);
        Assert.Equal(project.Id, result.Id);
        Assert.Single(result.Members);
        Assert.Equal(ownerId, result.Members[0].UserId);
    }

    [Fact]
    public async Task GetByIdAsync_存在しないIDでnullが返る()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    // ── GetByUserIdAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetByUserIdAsync_参加しているプロジェクトのみ返る()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var myProject = Project.Create("My Project", "説明", userId);
        var otherProject = Project.Create("Other Project", "説明", otherUserId);

        await _repository.AddAsync(myProject);
        await _repository.AddAsync(otherProject);

        var result = await _repository.GetByUserIdAsync(userId);

        Assert.Single(result);
        Assert.Equal(myProject.Id, result[0].Id);
    }

    [Fact]
    public async Task GetByUserIdAsync_メンバーとして招待されたプロジェクトも返る()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        var project = Project.Create("Project", "説明", ownerId);
        project.AddMember(memberId, MemberRole.Member);
        await _repository.AddAsync(project);

        var result = await _repository.GetByUserIdAsync(memberId);

        Assert.Single(result);
        Assert.Equal(project.Id, result[0].Id);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_メンバー追加後の変更が保存される()
    {
        var ownerId = Guid.NewGuid();
        var project = Project.Create("Project", "説明", ownerId);
        await _repository.AddAsync(project);

        var newMemberId = Guid.NewGuid();
        project.AddMember(newMemberId, MemberRole.Member);
        await _repository.UpdateAsync(project);

        var updated = await _repository.GetByIdAsync(project.Id);
        Assert.NotNull(updated);
        Assert.Equal(2, updated.Members.Count);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_プロジェクトが削除される()
    {
        var ownerId = Guid.NewGuid();
        var project = Project.Create("Project", "説明", ownerId);
        await _repository.AddAsync(project);

        await _repository.DeleteAsync(project.Id);

        var result = await _repository.GetByIdAsync(project.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_存在しないIDで例外が発生しない()
    {
        var exception = await Record.ExceptionAsync(
            () => _repository.DeleteAsync(Guid.NewGuid()));

        Assert.Null(exception);
    }
}
