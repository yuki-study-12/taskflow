using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;

namespace TaskFlow.Application.Boards.Queries;

public sealed record GetTaskCommentsQuery(Guid TaskId, Guid UserId);

public sealed class GetTaskCommentsQueryHandler(
    IBoardRepository boardRepository,
    IProjectRepository projectRepository)
    : IQueryHandler<GetTaskCommentsQuery, IReadOnlyList<CommentDto>>
{
    public async Task<IReadOnlyList<CommentDto>> HandleAsync(
        GetTaskCommentsQuery query,
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

        var comments = await boardRepository.GetCommentsByTaskIdAsync(query.TaskId, cancellationToken);

        return comments
            .Select(c => new CommentDto(c.Id, c.TaskId, c.AuthorId, c.Body, c.CreatedAt))
            .ToList();
    }
}
