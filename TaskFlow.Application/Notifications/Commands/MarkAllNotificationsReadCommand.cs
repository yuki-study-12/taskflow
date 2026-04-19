using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Application.Notifications.Commands;

public sealed record MarkAllNotificationsReadCommand(Guid UserId);

public sealed class MarkAllNotificationsReadCommandHandler(INotificationRepository notificationRepository)
    : ICommandHandler<MarkAllNotificationsReadCommand, bool>
{
    public async Task<bool> HandleAsync(
        MarkAllNotificationsReadCommand command,
        CancellationToken cancellationToken = default)
    {
        await notificationRepository.MarkAllAsReadAsync(command.UserId, cancellationToken);
        return true;
    }
}
