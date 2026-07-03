using Microsoft.AspNetCore.SignalR;

namespace BikeMate.Api.Hubs;

public sealed class LocationHub : Hub
{
    public Task JoinRequestLocationGroup(int requestId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, BookingHub.GetRequestGroup(requestId));
    }

    public Task LeaveRequestLocationGroup(int requestId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, BookingHub.GetRequestGroup(requestId));
    }
}
