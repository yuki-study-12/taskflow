using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;

namespace TaskFlow.Application.Projects.Queries;

public sealed record GetProjectMembersQuery(Guid ProjectId, Guid UserId);

public sealed class GetProjectMembersQueryHandler(IProjectRepository projectRepository)
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

        return project.Members
            .Select(m => new MemberDto(m.UserId, m.Role.Value))
            .ToList();
    }
}
