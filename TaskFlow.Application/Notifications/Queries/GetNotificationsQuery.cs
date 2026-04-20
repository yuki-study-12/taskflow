using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;

namespace TaskFlow.Application.Notifications.Queries;

public sealed record GetNotificationsQuery(Guid UserId);

public sealed class GetNotificationsQueryHandler(INotificationRepository notificationRepository)
    : IQueryHandler<GetNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    public async Task<IReadOnlyList<NotificationDto>> HandleAsync(
        GetNotificationsQuery query,
        CancellationToken cancellationToken = default)
    {
        var notifications = await notificationRepository.GetByUserIdAsync(query.UserId, cancellationToken);

        return notifications
            .Select(n => new NotificationDto(n.Id, n.Message, n.Type.ToString(), n.RelatedEntityId, n.IsRead, n.CreatedAt))
            .ToList();
    }
}
