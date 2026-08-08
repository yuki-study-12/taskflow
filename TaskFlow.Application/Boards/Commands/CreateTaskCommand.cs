using FluentValidation;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;
using TaskFlow.Domain.Boards;

namespace TaskFlow.Application.Boards.Commands;

public sealed record CreateTaskCommand(
    Guid ColumnId,
    string Title,
    string Description,
    Guid? AssigneeId,
    Guid UserId);

public sealed class CreateTaskCommandHandler(
    IBoardRepository boardRepository,
    IProjectRepository projectRepository)
    : ICommandHandler<CreateTaskCommand, TaskDto>
{
    public async Task<TaskDto> HandleAsync(
        CreateTaskCommand command,
        CancellationToken cancellationToken = default)
    {
        var column = await boardRepository.GetColumnByIdAsync(command.ColumnId, cancellationToken)
            ?? throw new NotFoundException("Column", command.ColumnId);

        var projectId = await boardRepository.GetProjectIdByColumnIdAsync(command.ColumnId, cancellationToken)
            ?? throw new NotFoundException("Column", command.ColumnId);

        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken)
            ?? throw new NotFoundException("Project", projectId);

        if (!project.Members.Any(m => m.UserId == command.UserId))
            throw new ForbiddenAccessException();

        var order = column.Tasks.Count;
        var task = BoardTask.Create(command.ColumnId, command.Title, command.Description, command.AssigneeId, order, command.UserId);
        await boardRepository.AddTaskAsync(task, cancellationToken);

        return new TaskDto(task.Id, projectId, task.ColumnId, task.Title, task.Description, task.AssigneeId, task.Order, task.DueDate, task.Priority, task.CreatedAt, task.UpdatedAt);
    }
}

public sealed class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}
