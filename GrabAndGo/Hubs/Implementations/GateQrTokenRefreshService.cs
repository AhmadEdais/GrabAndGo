namespace GrabAndGo.Api.Hubs.Implementations;

public class GateQrTokenRefreshService : IGateQrTokenRefreshService
{
    private readonly IHubContext<GateHub> _hubContext;
    public GateQrTokenRefreshService(IHubContext<GateHub> hubContext)
    {
        _hubContext = hubContext;
    }
    public async Task RefreshQrTokenAsync(int storeId)
    {
        await _hubContext.Clients.Group($"Gate_{storeId}")
            .SendAsync("RefreshQrToken");
    }
}
