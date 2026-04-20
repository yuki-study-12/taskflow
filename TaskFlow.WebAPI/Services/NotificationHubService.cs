using Microsoft.AspNetCore.SignalR;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.WebAPI.Hubs;

namespace TaskFlow.WebAPI.Services;

public sealed class NotificationHubService(IHubContext<NotificationHub> hubContext) : INotificationHubService
{
    public async Task SendUnreadCountAsync(Guid userId, int unreadCount, CancellationToken cancellationToken = default)
    {
        await hubContext.Clients
            .Group(userId.ToString())
            .SendAsync("UnreadCountUpdated", unreadCount, cancellationToken);
    }
}
