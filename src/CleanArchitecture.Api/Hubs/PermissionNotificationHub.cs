using Microsoft.AspNetCore.SignalR;

namespace CleanArchitecture.Api.Hubs;

/// <summary>
/// SignalR Hub for real-time permission notifications
/// </summary>
public class PermissionNotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            // Add user to a group with their userId for targeted notifications
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        }
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Client calls this when connected and authenticated to subscribe to updates
    /// </summary>
    public async Task SubscribeToPermissions(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
    }

    /// <summary>
    /// Client calls this to unsubscribe
    /// </summary>
    public async Task UnsubscribeFromPermissions(string userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
    }
}
