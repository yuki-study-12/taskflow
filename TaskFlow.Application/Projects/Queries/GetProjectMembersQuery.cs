using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;

namespace TaskFlow.Application.Projects.Queries;

public sealed record GetProjectMembersQuery(Guid ProjectId, Guid UserId);

public sealed class GetProjectMembersQueryHandler(
    IProjectRepository projectRepository,
    IIdentityService identityService)
    : IQueryHandler<GetProjectMembersQuery, IReadOnlyList<MemberDto>>
{
    public async Task<IReadOnlyList<MemberDto>> HandleAsync(
        GetProjectMembersQuery query,
        CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(query.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", query.ProjectId);

        if (!project.Members.Any(m => m.UserId == query.UserId))
            throw new ForbiddenAccessException();

        var memberIds = project.Members.Select(m => m.UserId).ToList();
        var users = await identityService.GetUsersByIdsAsync(memberIds, cancellationToken);
        var usersById = users.ToDictionary(u => u.Id);

        return project.Members
            .Select(m =>
            {
                usersById.TryGetValue(m.UserId, out var user);
                return new MemberDto(m.UserId, m.Role.Value, user?.DisplayName ?? "不明なユーザー", user?.AvatarUrl);
            })
            .ToList();
    }
}
