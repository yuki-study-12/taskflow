using TaskFlow.Domain.Notifications;
using Xunit;

namespace TaskFlow.Domain.Tests.Notifications;

public class NotificationTests
{
    [Fact]
    public void Create_ValidInput_ReturnsNotificationWithCorrectProperties()
    {
        var userId = Guid.NewGuid();
        var relatedEntityId = Guid.NewGuid();

        var notification = Notification.Create(userId, "タスクがアサインされました", NotificationType.TaskAssigned, relatedEntityId);

        Assert.Equal(userId, notification.UserId);
        Assert.Equal("タスクがアサインされました", notification.Message);
        Assert.Equal(NotificationType.TaskAssigned, notification.Type);
        Assert.Equal(relatedEntityId, notification.RelatedEntityId);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public void Create_EmptyUserId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => Notification.Create(Guid.Empty, "メッセージ", NotificationType.TaskAssigned, Guid.NewGuid()));
    }

    [Fact]
    public void Create_EmptyMessage_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => Notification.Create(Guid.NewGuid(), "   ", NotificationType.TaskAssigned, Guid.NewGuid()));
    }

    [Fact]
    public void MarkAsRead_未読の通知_IsReadがtrueになる()
    {
        var notification = Notification.Create(Guid.NewGuid(), "メッセージ", NotificationType.TaskCommented, Guid.NewGuid());

        notification.MarkAsRead();

        Assert.True(notification.IsRead);
    }

    [Fact]
    public void MarkAsRead_既読の通知に再度実行_IsReadはtrueのまま()
    {
        var notification = Notification.Create(Guid.NewGuid(), "メッセージ", NotificationType.MemberInvited, Guid.NewGuid());
        notification.MarkAsRead();

        notification.MarkAsRead();

        Assert.True(notification.IsRead);
    }
}
