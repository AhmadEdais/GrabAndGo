namespace GrabAndGo.Services.Interfaces
{
    public interface IGateNotificationService
    {
        Task SendCheckoutStatusAsync(CheckoutVisionResponseDto result);
    }
}