namespace GrabAndGo.Services
{
    public class GateNotificationService : IGateNotificationService
    {
        private readonly IHubContext<CartHub> _hubContext;

        public GateNotificationService(IHubContext<CartHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendCheckoutStatusAsync(CheckoutVisionResponseDto result)
        {
            var notification = new
            {
                Status = result.IsSuccess ? "SUCCESS" : "BLOCKED",
                Message = result.Message,
                Shortfall = result.ShortfallAmount,
                TransactionId = result.TransactionId
            };

            await _hubContext.Clients.Group(result.SessionId?.ToString())
                             .SendAsync("GateStatusUpdate", notification);
        }
    }
}