using Microsoft.AspNetCore.SignalR;

namespace BikeMate.Api.Hubs;

public sealed class EmergencyHub : Hub
{
    public Task JoinEmergencyRequestGroup(int requestId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, GetEmergencyGroup(requestId));
    }

    public Task LeaveEmergencyRequestGroup(int requestId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, GetEmergencyGroup(requestId));
    }

    public static string GetEmergencyGroup(int requestId)
    {
        return $"emergency-request-{requestId}";
    }
}
