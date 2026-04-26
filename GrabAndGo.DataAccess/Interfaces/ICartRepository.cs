namespace GrabAndGo.Application.Interfaces
{
    public interface ICartRepository
    {
        Task<CartSignalRDto> ProcessVisionEventAsync(VisionEventRequestDto requestDto);
    }
}