using GrabAndGo.Services.Interfaces;

namespace GrabAndGo.Services.Implementations
{
    public class VisionSystemService : IVisionSystemService
    {
        public readonly IVisionSystemRepository _visionRepo;
        private readonly ICartNotificationService _cartNotificationService;

        public VisionSystemService(IVisionSystemRepository visionRepo, ICartNotificationService cartNotificationService)
        {
            _visionRepo = visionRepo;
            _cartNotificationService = cartNotificationService;
        }   
        public async Task<BindTrackResponseDto?> BindTrackAsync(BindTrackRequestDto request)
        {
            var response = await _visionRepo.BindTrackAsync(request);
            await _cartNotificationService.BroadcastTrackBoundAsync(int.Parse(request.SessionId));
            return response;
        }
    }
}