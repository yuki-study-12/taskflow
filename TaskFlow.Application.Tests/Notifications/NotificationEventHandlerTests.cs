using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Notifications;
using TaskFlow.Domain.Boards;
using TaskFlow.Domain.Boards.Events;
using TaskFlow.Domain.Notifications;
using TaskFlow.Domain.Projects;
using TaskFlow.Domain.Projects.Events;
using TaskFlow.Domain.Tasks.Events;
using TaskFlow.Infrastructure.Persistence;
using Xunit;

namespace TaskFlow.Application.Tests.Notifications;

public class NotificationEventHandlerTests
{
    private readonly DbContextOptions<AppDbContext> _options;

    public NotificationEventHandlerTests()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private AppDbContext CreateContext() => new(_options);

    // ─── TaskAssignedEventHandler ───────────────────────────────

    [Fact]
    public async Task TaskAssignedEventHandler_通知が保存される()
    {
        var taskId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        var evt = new TaskAssignedEvent(taskId, assigneeId, DateTime.UtcNow);

        await using var ctx = CreateContext();
        var hub = new SpyNotificationHubService();
        var handler = new TaskAssignedEventHandler(new NotificationRepository(ctx), hub);

        await handler.HandleAsync(evt);

        await using var verify = CreateContext();
        var notifications = await verify.Notifications.ToListAsync();
        var n = Assert.Single(notifications);
        Assert.Equal(assigneeId, n.UserId);
        Assert.Equal(NotificationType.TaskAssigned, n.Type);
        Assert.Equal(taskId, n.RelatedEntityId);
        Assert.False(n.IsRead);
    }

    [Fact]
    public async Task TaskAssignedEventHandler_SignalRが未読数を送信する()
    {
        var assigneeId = Guid.NewGuid();
        var evt = new TaskAssignedEvent(Guid.NewGuid(), assigneeId, DateTime.UtcNow);

        await using var ctx = CreateContext();
        var hub = new SpyNotificationHubService();
        var handler = new TaskAssignedEventHandler(new NotificationRepository(ctx), hub);

        await handler.HandleAsync(evt);

        Assert.Single(hub.Calls);
        Assert.Equal(assigneeId, hub.Calls[0].UserId);
        Assert.Equal(1, hub.Calls[0].UnreadCount);
    }

    // ─── TaskCommentedEventHandler ──────────────────────────────

    [Fact]
    public async Task TaskCommentedEventHandler_担当者と作成者に通知が作成される()
    {
        var creatorId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        var authorId = Guid.NewGuid();

        var task = BoardTask.Create(Guid.NewGuid(), "タスク", "", assigneeId, 0, creatorId);
        await using var seedCtx = CreateContext();
        await new BoardRepository(seedCtx).AddTaskAsync(task);

        var evt = new TaskCommentedEvent(task.Id, Guid.NewGuid(), authorId, DateTime.UtcNow);

        await using var ctx = CreateContext();
        var hub = new SpyNotificationHubService();
        var handler = new TaskCommentedEventHandler(new BoardRepository(ctx), new NotificationRepository(ctx), hub);

        await handler.HandleAsync(evt);

        await using var verify = CreateContext();
        var notifications = await verify.Notifications.ToListAsync();
        Assert.Equal(2, notifications.Count);
        Assert.Contains(notifications, n => n.UserId == assigneeId);
        Assert.Contains(notifications, n => n.UserId == creatorId);
        Assert.All(notifications, n => Assert.Equal(NotificationType.TaskCommented, n.Type));
    }

    [Fact]
    public async Task TaskCommentedEventHandler_作成者がコメント者の場合は通知しない()
    {
        var authorId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();

        var task = BoardTask.Create(Guid.NewGuid(), "タスク", "", assigneeId, 0, authorId);
        await using var seedCtx = CreateContext();
        await new BoardRepository(seedCtx).AddTaskAsync(task);

        var evt = new TaskCommentedEvent(task.Id, Guid.NewGuid(), authorId, DateTime.UtcNow);

        await using var ctx = CreateContext();
        var hub = new SpyNotificationHubService();
        var handler = new TaskCommentedEventHandler(new BoardRepository(ctx), new NotificationRepository(ctx), hub);

        await handler.HandleAsync(evt);

        await using var verify = CreateContext();
        var notifications = await verify.Notifications.ToListAsync();
        var n = Assert.Single(notifications);
        Assert.Equal(assigneeId, n.UserId);
    }

    [Fact]
    public async Task TaskCommentedEventHandler_担当者がいない場合は作成者のみに通知()
    {
        var creatorId = Guid.NewGuid();
        var authorId = Guid.NewGuid();

        var task = BoardTask.Create(Guid.NewGuid(), "タスク", "", null, 0, creatorId);
        await using var seedCtx = CreateContext();
        await new BoardRepository(seedCtx).AddTaskAsync(task);

        var evt = new TaskCommentedEvent(task.Id, Guid.NewGuid(), authorId, DateTime.UtcNow);

        await using var ctx = CreateContext();
        var hub = new SpyNotificationHubService();
        var handler = new TaskCommentedEventHandler(new BoardRepository(ctx), new NotificationRepository(ctx), hub);

        await handler.HandleAsync(evt);

        await using var verify = CreateContext();
        var notifications = await verify.Notifications.ToListAsync();
        var n = Assert.Single(notifications);
        Assert.Equal(creatorId, n.UserId);
    }

    // ─── MemberInvitedEventHandler ──────────────────────────────

    [Fact]
    public async Task MemberInvitedEventHandler_招待されたユーザーに通知が作成される()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var evt = new MemberInvitedEvent(projectId, userId, MemberRole.Member, DateTime.UtcNow);

        await using var ctx = CreateContext();
        var hub = new SpyNotificationHubService();
        var handler = new MemberInvitedEventHandler(new NotificationRepository(ctx), hub);

        await handler.HandleAsync(evt);

        await using var verify = CreateContext();
        var notifications = await verify.Notifications.ToListAsync();
        var n = Assert.Single(notifications);
        Assert.Equal(userId, n.UserId);
        Assert.Equal(NotificationType.MemberInvited, n.Type);
        Assert.Equal(projectId, n.RelatedEntityId);
    }

    [Fact]
    public async Task MemberInvitedEventHandler_SignalRが未読数を送信する()
    {
        var userId = Guid.NewGuid();
        var evt = new MemberInvitedEvent(Guid.NewGuid(), userId, MemberRole.Admin, DateTime.UtcNow);

        await using var ctx = CreateContext();
        var hub = new SpyNotificationHubService();
        var handler = new MemberInvitedEventHandler(new NotificationRepository(ctx), hub);

        await handler.HandleAsync(evt);

        Assert.Single(hub.Calls);
        Assert.Equal(userId, hub.Calls[0].UserId);
    }

    // ─── テスト用スパイ ─────────────────────────────────────────

    private sealed class SpyNotificationHubService : INotificationHubService
    {
        public List<(Guid UserId, int UnreadCount)> Calls { get; } = [];

        public Task SendUnreadCountAsync(Guid userId, int unreadCount, CancellationToken cancellationToken = default)
        {
            Calls.Add((userId, unreadCount));
            return Task.CompletedTask;
        }
    }
}
