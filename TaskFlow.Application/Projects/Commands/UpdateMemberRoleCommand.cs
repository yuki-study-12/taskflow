using FluentValidation;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;
using TaskFlow.Domain.Projects;

namespace TaskFlow.Application.Projects.Commands;

public sealed record UpdateMemberRoleCommand(
    Guid ProjectId,
    Guid RequesterId,
    Guid TargetUserId,
    string Role);

public sealed class UpdateMemberRoleCommandHandler(IProjectRepository projectRepository)
    : ICommandHandler<UpdateMemberRoleCommand, MemberDto>
{
    public async Task<MemberDto> HandleAsync(
        UpdateMemberRoleCommand command,
        CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(command.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", command.ProjectId);

        var requester = project.Members.FirstOrDefault(m => m.UserId == command.RequesterId)
            ?? throw new ForbiddenAccessException();

        if (requester.Role != MemberRole.Owner && requester.Role != MemberRole.Admin)
            throw new ForbiddenAccessException();

        var newRole = MemberRole.FromValue(command.Role);
        project.UpdateMemberRole(command.TargetUserId, newRole);

        await projectRepository.UpdateAsync(project, cancellationToken);

        return new MemberDto(command.TargetUserId, newRole.Value);
    }
}

public sealed class UpdateMemberRoleCommandValidator : AbstractValidator<UpdateMemberRoleCommand>
{
    public UpdateMemberRoleCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.RequesterId).NotEmpty();
        RuleFor(x => x.TargetUserId).NotEmpty();
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(r => r is "Admin" or "Member")
            .WithMessage("Role must be 'Admin' or 'Member'.");
    }
}
