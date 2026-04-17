using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Application.Boards.Commands;

public sealed record DeleteTaskCommand(Guid TaskId, Guid UserId);

public sealed class DeleteTaskCommandHandler(
    IBoardRepository boardRepository,
    IProjectRepository projectRepository)
    : ICommandHandler<DeleteTaskCommand, bool>
{
    public async Task<bool> HandleAsync(
        DeleteTaskCommand command,
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

        await boardRepository.DeleteTaskAsync(command.TaskId, cancellationToken);
        return true;
    }
}
