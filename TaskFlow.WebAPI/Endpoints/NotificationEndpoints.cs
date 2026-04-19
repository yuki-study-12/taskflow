using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using TaskFlow.Application.Common.Constants;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;
using TaskFlow.Application.Notifications.Commands;
using TaskFlow.Application.Notifications.Queries;
using TaskFlow.Contracts.Notifications;

namespace TaskFlow.WebAPI.Endpoints;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();

        group.MapGet("/", GetNotificationsAsync)
             .WithName("GetNotifications");

        group.MapPut("/{id:guid}/read", MarkAsReadAsync)
             .WithName("MarkNotificationRead");

        group.MapPut("/read-all", MarkAllAsReadAsync)
             .WithName("MarkAllNotificationsRead");

        return app;
    }

    private static async Task<Results<Ok<IReadOnlyList<NotificationResponse>>, UnauthorizedHttpResult>>
        GetNotificationsAsync(
            ClaimsPrincipal principal,
            IQueryHandler<GetNotificationsQuery, IReadOnlyList<NotificationDto>> handler,
            CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
            return TypedResults.Unauthorized();

        var notifications = await handler.HandleAsync(new GetNotificationsQuery(userId), cancellationToken);

        var response = notifications
            .Select(n => new NotificationResponse(n.Id, n.Message, n.Type, n.RelatedEntityId, n.IsRead, n.CreatedAt))
            .ToList();

        return TypedResults.Ok<IReadOnlyList<NotificationResponse>>(response);
    }

    private static async Task<Results<Ok<NotificationResponse>, UnauthorizedHttpResult>>
        MarkAsReadAsync(
            Guid id,
            ClaimsPrincipal principal,
            ICommandHandler<MarkNotificationReadCommand, NotificationDto> handler,
            CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
            return TypedResults.Unauthorized();

        var notification = await handler.HandleAsync(new MarkNotificationReadCommand(id, userId), cancellationToken);

        return TypedResults.Ok(new NotificationResponse(notification.Id, notification.Message, notification.Type, notification.RelatedEntityId, notification.IsRead, notification.CreatedAt));
    }

    private static async Task<Results<NoContent, UnauthorizedHttpResult>>
        MarkAllAsReadAsync(
            ClaimsPrincipal principal,
            ICommandHandler<MarkAllNotificationsReadCommand, bool> handler,
            CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
            return TypedResults.Unauthorized();

        await handler.HandleAsync(new MarkAllNotificationsReadCommand(userId), cancellationToken);

        return TypedResults.NoContent();
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId)
    {
        var idClaim = principal.FindFirstValue(AppClaimTypes.UserId);
        return Guid.TryParse(idClaim, out userId);
    }
}
