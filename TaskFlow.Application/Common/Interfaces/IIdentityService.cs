using TaskFlow.Application.Common.Models;

namespace TaskFlow.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<AuthResult> RegisterAsync(
        string email,
        string password,
        string displayName,
        CancellationToken cancellationToken = default);

    Task<AuthResult> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<UserDto?> GetUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
