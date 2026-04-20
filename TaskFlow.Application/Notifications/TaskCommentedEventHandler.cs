using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Boards.Events;
using TaskFlow.Domain.Notifications;

namespace TaskFlow.Application.Notifications;

public sealed class TaskCommentedEventHandler(
    IBoardRepository boardRepository,
    INotificationRepository notificationRepository,
    INotificationHubService hubService)
    : INotificationHandler<TaskCommentedEvent>
{
    public async Task HandleAsync(TaskCommentedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var task = await boardRepository.GetTaskByIdAsync(domainEvent.TaskId, cancellationToken);
        if (task is null) return;

        var recipients = new HashSet<Guid>();

        if (task.AssigneeId.HasValue && task.AssigneeId.Value != domainEvent.AuthorId)
            recipients.Add(task.AssigneeId.Value);

        if (task.CreatorId != Guid.Empty && task.CreatorId != domainEvent.AuthorId)
            recipients.Add(task.CreatorId);

        foreach (var userId in recipients)
        {
            var notification = Notification.Create(
                userId,
                "タスクにコメントが追加されました",
                NotificationType.TaskCommented,
                domainEvent.TaskId);

            await notificationRepository.AddAsync(notification, cancellationToken);

            var unreadCount = await notificationRepository.GetUnreadCountAsync(userId, cancellationToken);
            await hubService.SendUnreadCountAsync(userId, unreadCount, cancellationToken);
        }
    }
}
