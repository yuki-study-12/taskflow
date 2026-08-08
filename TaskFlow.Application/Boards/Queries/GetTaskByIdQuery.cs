using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;

namespace TaskFlow.Application.Boards.Queries;

public sealed record GetTaskByIdQuery(Guid TaskId, Guid UserId);

public sealed class GetTaskByIdQueryHandler(
    IBoardRepository boardRepository,
    IProjectRepository projectRepository)
    : IQueryHandler<GetTaskByIdQuery, TaskDto>
{
    public async Task<TaskDto> HandleAsync(
        GetTaskByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var task = await boardRepository.GetTaskByIdAsync(query.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", query.TaskId);

        var projectId = await boardRepository.GetProjectIdByTaskIdAsync(query.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", query.TaskId);

        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken)
            ?? throw new NotFoundException("Project", projectId);

        if (!project.Members.Any(m => m.UserId == query.UserId))
            throw new ForbiddenAccessException();

        return new TaskDto(task.Id, projectId, task.ColumnId, task.Title, task.Description, task.AssigneeId, task.Order, task.DueDate, task.Priority, task.CreatedAt, task.UpdatedAt);
    }
}
