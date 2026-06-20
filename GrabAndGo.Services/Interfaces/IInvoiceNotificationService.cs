namespace GrabAndGo.Services.Interfaces;

public interface IInvoiceNotificationService
{
    Task SendInvoiceNotification(int transactionId);
}