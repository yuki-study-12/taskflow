using FluentValidation;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;

namespace TaskFlow.Application.Boards.Commands;

public sealed record MoveTaskCommand(
    Guid TaskId,
    Guid TargetColumnId,
    int NewOrder,
    Guid UserId);

public sealed class MoveTaskCommandHandler(
    IBoardRepository boardRepository,
    IProjectRepository projectRepository)
    : ICommandHandler<MoveTaskCommand, TaskDto>
{
    public async Task<TaskDto> HandleAsync(
        MoveTaskCommand command,
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

        _ = await boardRepository.GetColumnByIdAsync(command.TargetColumnId, cancellationToken)
            ?? throw new NotFoundException("Column", command.TargetColumnId);

        bool isSameColumn = task.ColumnId == command.TargetColumnId;

        if (isSameColumn)
        {
            var columnTasks = (await boardRepository.GetTasksByColumnIdAsync(command.TargetColumnId, cancellationToken))
                .Where(t => t.Id != command.TaskId)
                .ToList();

            var clampedOrder = Math.Clamp(command.NewOrder, 0, columnTasks.Count);
            columnTasks.Insert(clampedOrder, task);

            for (int i = 0; i < columnTasks.Count; i++)
                columnTasks[i].Move(command.TargetColumnId, i);
        }
        else
        {
            var sourceTasks = (await boardRepository.GetTasksByColumnIdAsync(task.ColumnId, cancellationToken))
                .Where(t => t.Id != command.TaskId)
                .ToList();

            for (int i = 0; i < sourceTasks.Count; i++)
                sourceTasks[i].Move(sourceTasks[i].ColumnId, i);

            var targetTasks = (await boardRepository.GetTasksByColumnIdAsync(command.TargetColumnId, cancellationToken))
                .ToList();

            var clampedOrder = Math.Clamp(command.NewOrder, 0, targetTasks.Count);
            targetTasks.Insert(clampedOrder, task);

            for (int i = 0; i < targetTasks.Count; i++)
                targetTasks[i].Move(command.TargetColumnId, i);
        }

        await boardRepository.UpdateTaskAsync(task, cancellationToken);

        return new TaskDto(task.Id, task.ColumnId, task.Title, task.Description, task.AssigneeId, task.Order, task.CreatedAt, task.UpdatedAt);
    }
}

public sealed class MoveTaskCommandValidator : AbstractValidator<MoveTaskCommand>
{
    public MoveTaskCommandValidator()
    {
        RuleFor(x => x.TargetColumnId).NotEmpty();
        RuleFor(x => x.NewOrder).GreaterThanOrEqualTo(0);
    }
}
