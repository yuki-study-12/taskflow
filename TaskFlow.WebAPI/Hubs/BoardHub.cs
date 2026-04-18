using Microsoft.AspNetCore.SignalR;

namespace TaskFlow.WebAPI.Hubs;

public sealed class BoardHub : Hub
{
    public async Task JoinBoard(string boardId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, boardId);
    }

    public async Task LeaveBoard(string boardId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, boardId);
    }
}
