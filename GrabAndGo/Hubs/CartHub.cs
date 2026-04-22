using Microsoft.AspNetCore.SignalR;

namespace GrabAndGo.Api.Hubs
{
    public class CartHub : Hub
    {
        public async Task SubscribeToSession(int sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Session_{sessionId}");
        }

        public async Task UnsubscribeFromSession(int sessionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Session_{sessionId}");
        }
    }
}