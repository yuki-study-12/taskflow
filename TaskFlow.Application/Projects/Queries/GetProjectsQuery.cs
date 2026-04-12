using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;

namespace TaskFlow.Application.Projects.Queries;

public sealed record GetProjectsQuery(Guid UserId);

public sealed class GetProjectsQueryHandler(IProjectRepository projectRepository)
    : IQueryHandler<GetProjectsQuery, IReadOnlyList<ProjectDto>>
{
    public async Task<IReadOnlyList<ProjectDto>> HandleAsync(
        GetProjectsQuery query,
        CancellationToken cancellationToken = default)
    {
        var projects = await projectRepository.GetByUserIdAsync(query.UserId, cancellationToken);

        return projects
            .Select(p =>
            {
                var member = p.Members.First(m => m.UserId == query.UserId);
                return new ProjectDto(p.Id, p.Name, p.Description, member.Role.Value);
            })
            .ToList();
    }
}
