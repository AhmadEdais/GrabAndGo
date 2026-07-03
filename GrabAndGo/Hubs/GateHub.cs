namespace GrabAndGo.Api.Hubs;
public class GateHub : Hub
{
    private readonly IStoreService _storeService;
    public GateHub(IStoreService storeService)
    {
        _storeService = storeService;
    }
    public async Task JoinGateGroup(int storeId)
    {
        var Exists = await _storeService.StoreExistsAsync(storeId);
        if(!Exists)
        {
            await Clients.Caller.SendAsync("Error", "Invalid store.");
            return;
        }
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Gate_{storeId}");
    }
    public async Task LeaveGateGroup(int storeId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Gate_{storeId}");
    }

}
