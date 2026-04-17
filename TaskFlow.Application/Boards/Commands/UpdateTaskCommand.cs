using FluentValidation;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;

namespace TaskFlow.Application.Boards.Commands;

public sealed record UpdateTaskCommand(
    Guid TaskId,
    string Title,
    string Description,
    Guid? AssigneeId,
    Guid UserId);

public sealed class UpdateTaskCommandHandler(
    IBoardRepository boardRepository,
    IProjectRepository projectRepository)
    : ICommandHandler<UpdateTaskCommand, TaskDto>
{
    public async Task<TaskDto> HandleAsync(
        UpdateTaskCommand command,
        CancellationToken cancellationToken = default)
    {
        var task = await boardRepository.GetTaskByIdAsync(command.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", command.TaskId);

        var projectId = await boardRepository.GetProjectIdByTaskIdAsync(command.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", command.TaskId);

        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken)
            ?? throw new NotFoundException("Project", projectId);

        if (!project.Members.Any(m => m.UserId == command.UserId))
            throw new ForbiddenAccessException();

        task.Update(command.Title, command.Description, command.AssigneeId);
        await boardRepository.UpdateTaskAsync(task, cancellationToken);

        return new TaskDto(task.Id, task.ColumnId, task.Title, task.Description, task.AssigneeId, task.Order, task.CreatedAt, task.UpdatedAt);
    }
}

public sealed class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}
