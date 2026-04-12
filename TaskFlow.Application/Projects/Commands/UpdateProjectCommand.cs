using FluentValidation;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;

namespace TaskFlow.Application.Projects.Commands;

public sealed record UpdateProjectCommand(
    Guid ProjectId,
    string Name,
    string Description,
    Guid UserId);

public sealed class UpdateProjectCommandHandler(IProjectRepository projectRepository)
    : ICommandHandler<UpdateProjectCommand, ProjectDto>
{
    public async Task<ProjectDto> HandleAsync(
        UpdateProjectCommand command,
        CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(command.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", command.ProjectId);

        var member = project.Members.FirstOrDefault(m => m.UserId == command.UserId)
            ?? throw new ForbiddenAccessException();

        project.Update(command.Name, command.Description);
        await projectRepository.UpdateAsync(project, cancellationToken);

        return new ProjectDto(
            project.Id,
            project.Name,
            project.Description,
            member.Role.Value);
    }
}

public sealed class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(1000);
    }
}
