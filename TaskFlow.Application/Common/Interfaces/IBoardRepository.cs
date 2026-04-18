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
    Task<IReadOnlyList<BoardTask>> GetTasksByColumnIdAsync(Guid columnId, CancellationToken cancellationToken = default);
    Task AddTaskAsync(BoardTask task, CancellationToken cancellationToken = default);
    Task UpdateTaskAsync(BoardTask task, CancellationToken cancellationToken = default);
    Task DeleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<Guid?> GetProjectIdByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default);
}
