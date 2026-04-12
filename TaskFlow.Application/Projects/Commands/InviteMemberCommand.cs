using FluentValidation;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;
using TaskFlow.Domain.Projects;

namespace TaskFlow.Application.Projects.Commands;

public sealed record InviteMemberCommand(
    Guid ProjectId,
    Guid RequesterId,
    Guid TargetUserId,
    string Role);

public sealed class InviteMemberCommandHandler(IProjectRepository projectRepository)
    : ICommandHandler<InviteMemberCommand, MemberDto>
{
    public async Task<MemberDto> HandleAsync(
        InviteMemberCommand command,
        CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(command.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", command.ProjectId);

        var requester = project.Members.FirstOrDefault(m => m.UserId == command.RequesterId)
            ?? throw new ForbiddenAccessException();

        if (requester.Role != MemberRole.Owner && requester.Role != MemberRole.Admin)
            throw new ForbiddenAccessException();

        var role = MemberRole.FromValue(command.Role);
        project.AddMember(command.TargetUserId, role);

        await projectRepository.UpdateAsync(project, cancellationToken);

        return new MemberDto(command.TargetUserId, role.Value);
    }
}

public sealed class InviteMemberCommandValidator : AbstractValidator<InviteMemberCommand>
{
    public InviteMemberCommandValidator()
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
