using Microsoft.AspNetCore.SignalR;

namespace TaskFlow.WebAPI.Hubs;

public sealed class NotificationHub : Hub
{
    public async Task JoinUserGroup(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, userId);
    }

    public async Task LeaveUserGroup(string userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
    }
}
