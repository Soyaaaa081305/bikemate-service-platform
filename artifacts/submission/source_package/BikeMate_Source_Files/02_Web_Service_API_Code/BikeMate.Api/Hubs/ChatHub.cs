using Microsoft.AspNetCore.SignalR;

namespace BikeMate.Api.Hubs;

public sealed class ChatHub : Hub
{
    public Task JoinConversation(string conversationId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, GetConversationGroup(conversationId));
    }

    public Task LeaveConversation(string conversationId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, GetConversationGroup(conversationId));
    }

    public Task SendTyping(string conversationId)
    {
        return Clients
            .OthersInGroup(GetConversationGroup(conversationId))
            .SendAsync("UserTyping", conversationId, Context.UserIdentifier);
    }

    private static string GetConversationGroup(string conversationId)
    {
        return $"conversation:{conversationId}";
    }
}
