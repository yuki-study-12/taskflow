using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;

namespace TaskFlow.Application.Notifications.Commands;

public sealed record MarkNotificationReadCommand(Guid NotificationId, Guid UserId);

public sealed class MarkNotificationReadCommandHandler(INotificationRepository notificationRepository)
    : ICommandHandler<MarkNotificationReadCommand, NotificationDto>
{
    public async Task<NotificationDto> HandleAsync(
        MarkNotificationReadCommand command,
        CancellationToken cancellationToken = default)
    {
        var notification = await notificationRepository.GetByIdAsync(command.NotificationId, cancellationToken)
            ?? throw new NotFoundException("Notification", command.NotificationId);

        if (notification.UserId != command.UserId)
            throw new ForbiddenAccessException();

        notification.MarkAsRead();
        await notificationRepository.SaveChangesAsync(cancellationToken);

        return new NotificationDto(notification.Id, notification.Message, notification.Type.ToString(), notification.RelatedEntityId, notification.IsRead, notification.CreatedAt);
    }
}
