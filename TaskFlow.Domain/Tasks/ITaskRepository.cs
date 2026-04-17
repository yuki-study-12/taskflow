namespace TaskFlow.Domain.Tasks;

public interface ITaskRepository
{
    System.Threading.Tasks.Task<TaskItem?> FindByIdAsync(Guid id, CancellationToken ct = default);
    System.Threading.Tasks.Task AddAsync(TaskItem task, CancellationToken ct = default);
    System.Threading.Tasks.Task UpdateAsync(TaskItem task, CancellationToken ct = default);
    System.Threading.Tasks.Task DeleteAsync(Guid id, CancellationToken ct = default);
    System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<TaskItem>> ListByColumnAsync(Guid columnId, CancellationToken ct = default);
}
