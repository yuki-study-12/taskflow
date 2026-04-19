using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Notifications.Queries;
using TaskFlow.Domain.Notifications;
using TaskFlow.Infrastructure.Persistence;
using Xunit;

namespace TaskFlow.Application.Tests.Notifications;

public class GetNotificationsQueryTests
{
    private readonly DbContextOptions<AppDbContext> _options;

    public GetNotificationsQueryTests()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private AppDbContext CreateContext() => new(_options);

    private async Task SeedNotificationsAsync(Guid userId, int count)
    {
        await using var ctx = CreateContext();
        for (var i = 0; i < count; i++)
        {
            var n = Notification.Create(userId, $"通知{i}", NotificationType.TaskAssigned, Guid.NewGuid());
            await ctx.Notifications.AddAsync(n);
        }
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task HandleAsync_ユーザーの通知一覧を返す()
    {
        var userId = Guid.NewGuid();
        await SeedNotificationsAsync(userId, 3);

        await using var ctx = CreateContext();
        var handler = new GetNotificationsQueryHandler(new NotificationRepository(ctx));
        var result = await handler.HandleAsync(new GetNotificationsQuery(userId));

        Assert.Equal(3, result.Count);
        Assert.All(result, n => Assert.False(n.IsRead));
    }

    [Fact]
    public async Task HandleAsync_他ユーザーの通知を含まない()
    {
        var userId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        await SeedNotificationsAsync(userId, 2);
        await SeedNotificationsAsync(otherId, 5);

        await using var ctx = CreateContext();
        var handler = new GetNotificationsQueryHandler(new NotificationRepository(ctx));
        var result = await handler.HandleAsync(new GetNotificationsQuery(userId));

        Assert.Equal(2, result.Count);
        Assert.All(result, n => Assert.Equal("TaskAssigned", n.Type));
    }

    [Fact]
    public async Task HandleAsync_通知がない場合は空リストを返す()
    {
        await using var ctx = CreateContext();
        var handler = new GetNotificationsQueryHandler(new NotificationRepository(ctx));
        var result = await handler.HandleAsync(new GetNotificationsQuery(Guid.NewGuid()));

        Assert.Empty(result);
    }
}
