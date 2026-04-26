namespace GrabAndGo.Services.Implementations
{
    public class VisionSystemService : IVisionSystemService
    {
        public readonly IVisionSystemRepository _visionRepo;

        public VisionSystemService(IVisionSystemRepository visionRepo)
        {
            _visionRepo = visionRepo;
        }   
        public async Task<BindTrackResponseDto?> BindTrackAsync(BindTrackRequestDto request)
        {
            return await _visionRepo.BindTrackAsync(request);
        }
    }
}