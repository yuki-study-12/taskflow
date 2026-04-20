namespace TaskFlow.Domain.Notifications;

public class Notification : Common.Entity<Guid>
{
    public Guid UserId { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public NotificationType Type { get; private set; }
    public Guid RelatedEntityId { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Notification(Guid id, Guid userId, string message, NotificationType type, Guid relatedEntityId, DateTime createdAt)
        : base(id)
    {
        UserId = userId;
        Message = message;
        Type = type;
        RelatedEntityId = relatedEntityId;
        IsRead = false;
        CreatedAt = createdAt;
    }

    // EF Core 用
    private Notification() { }

    public static Notification Create(Guid userId, string message, NotificationType type, Guid relatedEntityId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be empty.", nameof(message));

        return new Notification(Guid.NewGuid(), userId, message, type, relatedEntityId, DateTime.UtcNow);
    }

    public void MarkAsRead() => IsRead = true;
}
