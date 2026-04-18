using TaskFlow.Application.Common.Models;

namespace TaskFlow.Application.Common.Interfaces;

public interface IBoardNotificationService
{
    Task NotifyTaskMovedAsync(Guid boardId, TaskDto task, CancellationToken cancellationToken = default);
}
