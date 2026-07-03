using Microsoft.AspNetCore.SignalR;

namespace BikeMate.Api.Hubs;

public sealed class NotificationHub : Hub
{
    public Task JoinUserGroup(int userId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroup(userId));
    }

    public Task LeaveUserGroup(int userId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, GetUserGroup(userId));
    }

    public static string GetUserGroup(int userId)
    {
        return $"user-{userId}";
    }
}
