namespace TaskFlow.Contracts.Notifications;

public sealed record NotificationResponse(
    Guid Id,
    string Message,
    string Type,
    Guid RelatedEntityId,
    bool IsRead,
    DateTime CreatedAt);
