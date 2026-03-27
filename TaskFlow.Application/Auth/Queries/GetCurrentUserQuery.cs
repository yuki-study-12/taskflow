using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;

namespace TaskFlow.Application.Auth.Queries;

public sealed record GetCurrentUserQuery(Guid UserId);

public sealed class GetCurrentUserQueryHandler(IIdentityService identityService)
    : IQueryHandler<GetCurrentUserQuery, UserDto?>
{
    public Task<UserDto?> HandleAsync(
        GetCurrentUserQuery query,
        CancellationToken cancellationToken = default) =>
        identityService.GetUserByIdAsync(query.UserId, cancellationToken);
}
