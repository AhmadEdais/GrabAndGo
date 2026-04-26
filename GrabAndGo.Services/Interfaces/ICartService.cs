namespace GrabAndGo.Services.Interfaces
{
    public interface ICartService
    {
        Task<CartSignalRDto> ProcessVisionEventAsync(VisionEventRequestDto visionEvent);
    }
}
