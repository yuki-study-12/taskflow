using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;

namespace TaskFlow.Application.Projects.Queries;

public sealed record GetProjectByIdQuery(Guid ProjectId, Guid UserId);

public sealed class GetProjectByIdQueryHandler(IProjectRepository projectRepository)
    : IQueryHandler<GetProjectByIdQuery, ProjectDto>
{
    public async Task<ProjectDto> HandleAsync(
        GetProjectByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(query.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", query.ProjectId);

        var member = project.Members.FirstOrDefault(m => m.UserId == query.UserId)
            ?? throw new ForbiddenAccessException();

        return new ProjectDto(project.Id, project.Name, project.Description, member.Role.Value);
    }
}
