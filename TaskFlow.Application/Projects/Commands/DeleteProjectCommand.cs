using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Projects;

namespace TaskFlow.Application.Projects.Commands;

public sealed record DeleteProjectCommand(
    Guid ProjectId,
    Guid UserId);

public sealed class DeleteProjectCommandHandler(IProjectRepository projectRepository)
    : ICommandHandler<DeleteProjectCommand, bool>
{
    public async Task<bool> HandleAsync(
        DeleteProjectCommand command,
        CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(command.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", command.ProjectId);

        var member = project.Members.FirstOrDefault(m => m.UserId == command.UserId);
        if (member is null || member.Role != MemberRole.Owner)
            throw new ForbiddenAccessException();

        await projectRepository.DeleteAsync(command.ProjectId, cancellationToken);
        return true;
    }
}
