using Microsoft.AspNetCore.SignalR;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;
using TaskFlow.WebAPI.Hubs;

namespace TaskFlow.WebAPI.Services;

public sealed class BoardHubNotificationService(IHubContext<BoardHub> hubContext) : IBoardNotificationService
{
    public async Task NotifyTaskMovedAsync(Guid boardId, TaskDto task, CancellationToken cancellationToken = default)
    {
        await hubContext.Clients
            .Group(boardId.ToString())
            .SendAsync("TaskMoved", new
            {
                task.Id,
                task.ColumnId,
                task.Title,
                task.Description,
                task.AssigneeId,
                task.Order,
            }, cancellationToken);
    }
}
