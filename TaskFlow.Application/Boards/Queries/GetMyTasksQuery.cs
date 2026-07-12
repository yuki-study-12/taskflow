using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;

namespace TaskFlow.Application.Boards.Queries;

public sealed record GetMyTasksQuery(Guid UserId);

public sealed class GetMyTasksQueryHandler(
    IBoardRepository boardRepository,
    IProjectRepository projectRepository)
    : IQueryHandler<GetMyTasksQuery, IReadOnlyList<MyTaskDto>>
{
    public async Task<IReadOnlyList<MyTaskDto>> HandleAsync(
        GetMyTasksQuery query,
        CancellationToken cancellationToken = default)
    {
        var tasks = await boardRepository.GetTasksByAssigneeIdAsync(query.UserId, cancellationToken);
        if (tasks.Count == 0)
            return [];

        var columnIds = tasks.Select(t => t.ColumnId).Distinct().ToList();
        var columns = await boardRepository.GetColumnsByIdsAsync(columnIds, cancellationToken);
        var columnsById = columns.ToDictionary(c => c.Id);

        var boardIds = columns.Select(c => c.BoardId).Distinct().ToList();
        var boards = await boardRepository.GetBoardsByIdsAsync(boardIds, cancellationToken);
        var projectIdByBoardId = boards.ToDictionary(b => b.Id, b => b.ProjectId);

        var projects = await projectRepository.GetByUserIdAsync(query.UserId, cancellationToken);
        var projectNameById = projects.ToDictionary(p => p.Id, p => p.Name);

        var result = new List<MyTaskDto>();
        foreach (var task in tasks)
        {
            if (!columnsById.TryGetValue(task.ColumnId, out var column))
                continue;
            if (!projectIdByBoardId.TryGetValue(column.BoardId, out var projectId))
                continue;
            if (!projectNameById.TryGetValue(projectId, out var projectName))
                continue;

            result.Add(new MyTaskDto(
                task.Id,
                task.Title,
                task.Description,
                projectId,
                projectName,
                column.Id,
                column.Name,
                task.CreatedAt,
                task.UpdatedAt));
        }

        return result;
    }
}
