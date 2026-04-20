using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Notifications;
using TaskFlow.Domain.Tasks.Events;

namespace TaskFlow.Application.Notifications;

public sealed class TaskAssignedEventHandler(
    INotificationRepository notificationRepository,
    INotificationHubService hubService)
    : INotificationHandler<TaskAssignedEvent>
{
    public async Task HandleAsync(TaskAssignedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var notification = Notification.Create(
            domainEvent.AssigneeId,
            "タスクがアサインされました",
            NotificationType.TaskAssigned,
            domainEvent.TaskId);

        await notificationRepository.AddAsync(notification, cancellationToken);

        var unreadCount = await notificationRepository.GetUnreadCountAsync(domainEvent.AssigneeId, cancellationToken);
        await hubService.SendUnreadCountAsync(domainEvent.AssigneeId, unreadCount, cancellationToken);
    }
}
