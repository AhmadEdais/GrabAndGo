using System.Threading.Tasks;
using GrabAndGo.Application.DTOs.Vision;

namespace GrabAndGo.Application.Interfaces
{
    public interface ICartRepository
    {
        Task<CartSignalRDto> ProcessVisionEventAsync(VisionEventRequestDto requestDto);
    }
}