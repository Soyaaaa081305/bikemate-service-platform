using Microsoft.AspNetCore.SignalR;

namespace BikeMate.Api.Hubs;

public sealed class BookingHub : Hub
{
    public Task JoinRequestGroup(int requestId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, GetRequestGroup(requestId));
    }

    public Task LeaveRequestGroup(int requestId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, GetRequestGroup(requestId));
    }

    public static string GetRequestGroup(int requestId)
    {
        return $"request-{requestId}";
    }
}
