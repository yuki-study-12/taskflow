using TaskFlow.Domain.Boards;

namespace TaskFlow.Application.Common.Interfaces;

public interface IBoardRepository
{
    // Board
    Task<Board?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Board?> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task AddAsync(Board board, CancellationToken cancellationToken = default);

    // Column
    Task<Column?> GetColumnByIdAsync(Guid columnId, CancellationToken cancellationToken = default);
    Task AddColumnAsync(Column column, CancellationToken cancellationToken = default);
    Task UpdateColumnAsync(Column column, CancellationToken cancellationToken = default);
    Task<Guid?> GetProjectIdByColumnIdAsync(Guid columnId, CancellationToken cancellationToken = default);

    // Task
    Task<BoardTask?> GetTaskByIdAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<BoardTask?> GetTaskWithCommentsAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BoardTask>> GetTasksByColumnIdAsync(Guid columnId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BoardTask>> GetTasksByAssigneeIdAsync(Guid assigneeId, CancellationToken cancellationToken = default);
    Task AddTaskAsync(BoardTask task, CancellationToken cancellationToken = default);
    Task UpdateTaskAsync(BoardTask task, CancellationToken cancellationToken = default);
    Task DeleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<Guid?> GetProjectIdByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default);

    // Batch lookups (read-side helpers)
    Task<IReadOnlyList<Column>> GetColumnsByIdsAsync(IReadOnlyCollection<Guid> columnIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Board>> GetBoardsByIdsAsync(IReadOnlyCollection<Guid> boardIds, CancellationToken cancellationToken = default);

    // Comment
    Task<TaskComment?> GetCommentByIdAsync(Guid commentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskComment>> GetCommentsByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task AddCommentAsync(TaskComment comment, CancellationToken cancellationToken = default);
    Task DeleteCommentAsync(Guid commentId, CancellationToken cancellationToken = default);
}
