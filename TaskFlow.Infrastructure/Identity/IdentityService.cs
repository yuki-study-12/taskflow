using Microsoft.AspNetCore.Identity;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;

namespace TaskFlow.Infrastructure.Identity;

internal sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    IJwtTokenService jwtTokenService) : IIdentityService
{
    public async Task<AuthResult> RegisterAsync(
        string email,
        string password,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
            return AuthResult.Failure(["このメールアドレスは既に使用されています。"]);

        var user = new ApplicationUser
        {
            UserName    = email,
            Email       = email,
            DisplayName = displayName,
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return AuthResult.Failure(result.Errors.Select(e => e.Description));

        var dto   = MapToDto(user);
        var token = jwtTokenService.GenerateToken(dto);
        return AuthResult.Success(dto, token);
    }

    public async Task<AuthResult> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return AuthResult.Failure(["メールアドレスまたはパスワードが正しくありません。"]);

        var valid = await userManager.CheckPasswordAsync(user, password);
        if (!valid)
            return AuthResult.Failure(["メールアドレスまたはパスワードが正しくありません。"]);

        var dto   = MapToDto(user);
        var token = jwtTokenService.GenerateToken(dto);
        return AuthResult.Success(dto, token);
    }

    public async Task<UserDto?> GetUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : MapToDto(user);
    }

    private static UserDto MapToDto(ApplicationUser user) =>
        new(user.Id, user.Email!, user.DisplayName, user.AvatarUrl);
}
