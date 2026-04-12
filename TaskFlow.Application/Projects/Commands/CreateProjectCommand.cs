using FluentValidation;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;
using TaskFlow.Domain.Projects;

namespace TaskFlow.Application.Projects.Commands;

public sealed record CreateProjectCommand(
    string Name,
    string Description,
    Guid UserId);

public sealed class CreateProjectCommandHandler(IProjectRepository projectRepository)
    : ICommandHandler<CreateProjectCommand, ProjectDto>
{
    public async Task<ProjectDto> HandleAsync(
        CreateProjectCommand command,
        CancellationToken cancellationToken = default)
    {
        var project = Project.Create(command.Name, command.Description, command.UserId);
        await projectRepository.AddAsync(project, cancellationToken);

        return new ProjectDto(
            project.Id,
            project.Name,
            project.Description,
            MemberRole.Owner.Value);
    }
}

public sealed class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(1000);
    }
}
