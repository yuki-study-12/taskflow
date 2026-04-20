namespace TaskFlow.Application.Common.Models;

public sealed record NotificationDto(
    Guid Id,
    string Message,
    string Type,
    Guid RelatedEntityId,
    bool IsRead,
    DateTime CreatedAt);
