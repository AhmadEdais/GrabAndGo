namespace GrabAndGo.Services.Interfaces
{
    public interface ICartService
    {
        Task<CartSignalRDto> ProcessVisionEventAsync(VisionEventRequestDto visionEvent);
        Task<CartSignalRDto?> GetActiveCartByUserIdAsync(int userId);
    }
}
