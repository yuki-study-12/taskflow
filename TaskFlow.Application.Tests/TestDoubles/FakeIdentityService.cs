using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;

namespace TaskFlow.Application.Tests.TestDoubles;

public sealed class FakeIdentityService : IIdentityService
{
    private readonly Dictionary<Guid, UserDto> _users = [];

    public FakeIdentityService WithUser(Guid userId, string displayName = "テストユーザー", string? avatarUrl = null)
    {
        _users[userId] = new UserDto(userId, $"{userId}@example.com", displayName, avatarUrl);
        return this;
    }

    public Task<AuthResult> RegisterAsync(string email, string password, string displayName, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<AuthResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.GetValueOrDefault(userId));

    public Task<IReadOnlyList<UserDto>> GetUsersByIdsAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<UserDto>>(
            userIds.Where(_users.ContainsKey).Select(id => _users[id]).ToList());
}
