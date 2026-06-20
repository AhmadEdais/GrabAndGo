namespace GrabAndGo.Api.Hubs
{
    [Authorize]

    public class InvoiceHub : Hub
    {
        private readonly ITransactionService _transactionService;

        public InvoiceHub(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }
        public async Task SubscribeToInvoice(int transactionId)
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                throw new HubException("Invalid token identity.");

            var owns = await _transactionService.DoesUserOwnTransactionAsync(userId, transactionId);
            if (!owns)
                throw new HubException("Cannot subscribe to a transaction you do not own.");

            await Groups.AddToGroupAsync(Context.ConnectionId, $"Invoice_{transactionId}");
        }
        public async Task UnsubscribeFromInvoice(int transactionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Invoice_{transactionId}");
        }
    }
}
