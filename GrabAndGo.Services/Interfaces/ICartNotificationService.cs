namespace GrabAndGo.Services.Interfaces
{
    public interface ICartNotificationService
    {
        Task BroadcastCartUpdateAsync(CartSignalRDto cartDto);
    }
}