using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Projects.Commands;
using TaskFlow.Domain.Projects;
using TaskFlow.Infrastructure.Persistence;
using Xunit;

namespace TaskFlow.Application.Tests.Projects.Commands;

public class CreateProjectCommandTests
{
    private readonly DbContextOptions<AppDbContext> _options;

    public CreateProjectCommandTests()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private AppDbContext CreateContext() => new(_options);

    // ── ハンドラー ──────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_正常なコマンド_ProjectDtoを返す()
    {
        var command = new CreateProjectCommand("新規プロジェクト", "説明文", Guid.NewGuid());

        await using var ctx = CreateContext();
        var result = await new CreateProjectCommandHandler(new ProjectRepository(ctx)).HandleAsync(command);

        Assert.Equal("新規プロジェクト", result.Name);
        Assert.Equal("説明文", result.Description);
        Assert.Equal(MemberRole.Owner.Value, result.UserRole);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task HandleAsync_正常なコマンド_プロジェクトがDBに保存される()
    {
        var userId = Guid.NewGuid();
        var command = new CreateProjectCommand("新規プロジェクト", "説明文", userId);

        await using (var ctx = CreateContext())
            await new CreateProjectCommandHandler(new ProjectRepository(ctx)).HandleAsync(command);

        await using var verify = CreateContext();
        var saved = await verify.Projects.Include(p => p.Members).FirstOrDefaultAsync();
        Assert.NotNull(saved);
        Assert.Equal("新規プロジェクト", saved.Name);
        Assert.Single(saved.Members);
        Assert.Equal(userId, saved.Members[0].UserId);
    }

    // ── バリデーター ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("", "説明文")]
    [InlineData("   ", "説明文")]
    [InlineData("プロジェクト名", "")]
    [InlineData("プロジェクト名", "   ")]
    public void Validate_空文字_バリデーションエラーになる(string name, string description)
    {
        var command = new CreateProjectCommand(name, description, Guid.NewGuid());
        var validator = new CreateProjectCommandValidator();

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_名前が200文字超_バリデーションエラーになる()
    {
        var command = new CreateProjectCommand(new string('a', 201), "説明文", Guid.NewGuid());
        var validator = new CreateProjectCommandValidator();

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.Name));
    }

    [Fact]
    public void Validate_説明が1000文字超_バリデーションエラーになる()
    {
        var command = new CreateProjectCommand("プロジェクト名", new string('a', 1001), Guid.NewGuid());
        var validator = new CreateProjectCommandValidator();

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.Description));
    }

    [Fact]
    public void Validate_正常な入力_バリデーション成功()
    {
        var command = new CreateProjectCommand("プロジェクト名", "説明文", Guid.NewGuid());
        var validator = new CreateProjectCommandValidator();

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
