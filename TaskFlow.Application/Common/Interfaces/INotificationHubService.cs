namespace TaskFlow.Application.Common.Interfaces;

public interface INotificationHubService
{
    Task SendUnreadCountAsync(Guid userId, int unreadCount, CancellationToken cancellationToken = default);
}
