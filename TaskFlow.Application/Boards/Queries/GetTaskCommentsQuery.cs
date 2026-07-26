using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;

namespace TaskFlow.Application.Boards.Queries;

public sealed record GetTaskCommentsQuery(Guid TaskId, Guid UserId);

public sealed class GetTaskCommentsQueryHandler(
    IBoardRepository boardRepository,
    IProjectRepository projectRepository,
    IIdentityService identityService)
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

        var authorIds = comments.Select(c => c.AuthorId).Distinct().ToList();
        var authors = await identityService.GetUsersByIdsAsync(authorIds, cancellationToken);
        var displayNameByAuthorId = authors.ToDictionary(a => a.Id, a => a.DisplayName);

        return comments
            .Select(c => new CommentDto(
                c.Id,
                c.TaskId,
                c.AuthorId,
                displayNameByAuthorId.GetValueOrDefault(c.AuthorId, "不明なユーザー"),
                c.Body,
                c.CreatedAt))
            .ToList();
    }
}
