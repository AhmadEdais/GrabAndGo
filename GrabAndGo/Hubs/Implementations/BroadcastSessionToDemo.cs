namespace GrabAndGo.Api.Hubs.Implementations;

public class BroadcastSessionToDemo : IBroadcastSessionToDemo
{
    private readonly IHubContext<CartHub> hubContext;
    public BroadcastSessionToDemo(IHubContext<CartHub> hubContext)
    {
        this.hubContext = hubContext;
    }
    public async Task BroadcastSessionEnteredAsync(int sessionId, int storeId, DateTime startedAt)
    {
        await hubContext.Clients.Group("DemoControllers")
            .SendAsync("SessionEntered", new { SessionId = sessionId, StoreId = storeId, StartedAt = startedAt });
    }
}
