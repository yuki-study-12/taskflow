using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Notifications.Commands;
using TaskFlow.Domain.Notifications;
using TaskFlow.Infrastructure.Persistence;
using Xunit;

namespace TaskFlow.Application.Tests.Notifications;

public class MarkNotificationReadCommandTests
{
    private readonly DbContextOptions<AppDbContext> _options;

    public MarkNotificationReadCommandTests()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private AppDbContext CreateContext() => new(_options);

    private async Task<Notification> SeedNotificationAsync(Guid userId)
    {
        var n = Notification.Create(userId, "テスト通知", NotificationType.TaskAssigned, Guid.NewGuid());
        await using var ctx = CreateContext();
        await ctx.Notifications.AddAsync(n);
        await ctx.SaveChangesAsync();
        return n;
    }

    [Fact]
    public async Task HandleAsync_既読に更新してDtoを返す()
    {
        var userId = Guid.NewGuid();
        var n = await SeedNotificationAsync(userId);

        await using var ctx = CreateContext();
        var handler = new MarkNotificationReadCommandHandler(new NotificationRepository(ctx));
        var result = await handler.HandleAsync(new MarkNotificationReadCommand(n.Id, userId));

        Assert.True(result.IsRead);
        Assert.Equal(n.Id, result.Id);

        await using var verify = CreateContext();
        var saved = await verify.Notifications.FindAsync(n.Id);
        Assert.True(saved!.IsRead);
    }

    [Fact]
    public async Task HandleAsync_存在しない通知_NotFoundExceptionをスロー()
    {
        await using var ctx = CreateContext();
        var handler = new MarkNotificationReadCommandHandler(new NotificationRepository(ctx));

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.HandleAsync(new MarkNotificationReadCommand(Guid.NewGuid(), Guid.NewGuid())));
    }

    [Fact]
    public async Task HandleAsync_別ユーザーの通知_ForbiddenAccessExceptionをスロー()
    {
        var ownerId = Guid.NewGuid();
        var n = await SeedNotificationAsync(ownerId);

        await using var ctx = CreateContext();
        var handler = new MarkNotificationReadCommandHandler(new NotificationRepository(ctx));

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.HandleAsync(new MarkNotificationReadCommand(n.Id, Guid.NewGuid())));
    }

    // ─── MarkAllNotificationsReadCommand ────────────────────────

    [Fact]
    public async Task MarkAllAsync_ユーザーの全通知が既読になる()
    {
        var userId = Guid.NewGuid();
        await SeedNotificationAsync(userId);
        await SeedNotificationAsync(userId);

        await using var ctx = CreateContext();
        var handler = new MarkAllNotificationsReadCommandHandler(new NotificationRepository(ctx));
        var result = await handler.HandleAsync(new MarkAllNotificationsReadCommand(userId));

        Assert.True(result);

        await using var verify = CreateContext();
        var notifications = verify.Notifications.Where(n => n.UserId == userId).ToList();
        Assert.All(notifications, n => Assert.True(n.IsRead));
    }

    [Fact]
    public async Task MarkAllAsync_他ユーザーの通知は変更しない()
    {
        var userId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var other = await SeedNotificationAsync(otherId);
        await SeedNotificationAsync(userId);

        await using var ctx = CreateContext();
        var handler = new MarkAllNotificationsReadCommandHandler(new NotificationRepository(ctx));
        await handler.HandleAsync(new MarkAllNotificationsReadCommand(userId));

        await using var verify = CreateContext();
        var otherNotification = await verify.Notifications.FindAsync(other.Id);
        Assert.False(otherNotification!.IsRead);
    }
}
