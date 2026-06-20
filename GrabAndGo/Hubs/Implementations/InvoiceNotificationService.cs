namespace GrabAndGo.Api.Hubs.Implementations;

public class InvoiceNotificationService : IInvoiceNotificationService
{
    private readonly IHubContext<InvoiceHub> _hubContext;

    public InvoiceNotificationService(IHubContext<InvoiceHub> hubContext)
    {
        _hubContext = hubContext;
    }
    public async Task SendInvoiceNotification(int transactionId)
    {
        await _hubContext.Clients.Group($"Invoice_{transactionId}")
                 .SendAsync("InvoicePdfReady", new { transactionId });
    }
}
