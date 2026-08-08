using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;

namespace TaskFlow.Application.Boards.Queries;

public sealed record GetBoardByProjectIdQuery(Guid ProjectId, Guid UserId);

public sealed class GetBoardByProjectIdQueryHandler(
    IBoardRepository boardRepository,
    IProjectRepository projectRepository)
    : IQueryHandler<GetBoardByProjectIdQuery, BoardDto>
{
    public async Task<BoardDto> HandleAsync(
        GetBoardByProjectIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(query.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", query.ProjectId);

        if (!project.Members.Any(m => m.UserId == query.UserId))
            throw new ForbiddenAccessException();

        var board = await boardRepository.GetByProjectIdAsync(query.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Board", query.ProjectId);

        var columns = board.Columns
            .OrderBy(c => c.Order)
            .Select(c => new ColumnDto(
                c.Id,
                c.BoardId,
                c.Name,
                c.Order,
                c.Tasks
                    .OrderBy(t => t.Order)
                    .Select(t => new TaskDto(
                        t.Id,
                        query.ProjectId,
                        t.ColumnId,
                        t.Title,
                        t.Description,
                        t.AssigneeId,
                        t.Order,
                        t.DueDate,
                        t.Priority,
                        t.CreatedAt,
                        t.UpdatedAt))
                    .ToList()))
            .ToList();

        return new BoardDto(board.Id, board.ProjectId, board.Name, columns);
    }
}
