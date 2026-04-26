namespace GrabAndGo.Api.Notifications
{
    public class SignalRCartNotificationService : ICartNotificationService
    {
        private readonly IHubContext<CartHub> _hubContext;

        public SignalRCartNotificationService(IHubContext<CartHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task BroadcastCartUpdateAsync(CartSignalRDto cartDto)
        {
            await _hubContext.Clients
                .Group($"Session_{cartDto.SessionId}")
                .SendAsync("ReceiveCartUpdate", cartDto);
        }
    }
}