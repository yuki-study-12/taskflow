using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Projects;

namespace TaskFlow.Application.Projects.Commands;

public sealed record RemoveMemberCommand(
    Guid ProjectId,
    Guid RequesterId,
    Guid TargetUserId);

public sealed class RemoveMemberCommandHandler(IProjectRepository projectRepository)
    : ICommandHandler<RemoveMemberCommand, bool>
{
    public async Task<bool> HandleAsync(
        RemoveMemberCommand command,
        CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(command.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", command.ProjectId);

        var requester = project.Members.FirstOrDefault(m => m.UserId == command.RequesterId)
            ?? throw new ForbiddenAccessException();

        if (requester.Role != MemberRole.Owner && requester.Role != MemberRole.Admin)
            throw new ForbiddenAccessException();

        project.RemoveMember(command.TargetUserId);

        await projectRepository.UpdateAsync(project, cancellationToken);

        return true;
    }
}
