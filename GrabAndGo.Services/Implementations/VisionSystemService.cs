using GrabAndGo.DataAccess.Interfaces;
using GrabAndGo.Models.Requests.Vision_System;
using GrabAndGo.Models.Responses.Vision_System;
using GrabAndGo.Services.Interfaces;

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