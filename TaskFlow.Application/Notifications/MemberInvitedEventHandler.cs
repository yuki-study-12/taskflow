using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Notifications;
using TaskFlow.Domain.Projects.Events;

namespace TaskFlow.Application.Notifications;

public sealed class MemberInvitedEventHandler(
    INotificationRepository notificationRepository,
    INotificationHubService hubService)
    : INotificationHandler<MemberInvitedEvent>
{
    public async Task HandleAsync(MemberInvitedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var notification = Notification.Create(
            domainEvent.UserId,
            "プロジェクトに招待されました",
            NotificationType.MemberInvited,
            domainEvent.ProjectId);

        await notificationRepository.AddAsync(notification, cancellationToken);

        var unreadCount = await notificationRepository.GetUnreadCountAsync(domainEvent.UserId, cancellationToken);
        await hubService.SendUnreadCountAsync(domainEvent.UserId, unreadCount, cancellationToken);
    }
}
