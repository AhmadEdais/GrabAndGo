using GrabAndGo.Models.Requests.Vision_System;
using GrabAndGo.Models.Responses.Vision_System;

namespace GrabAndGo.Services.Interfaces
{
    public interface IVisionSystemService
    {
            Task<BindTrackResponseDto?> BindTrackAsync(BindTrackRequestDto request);
    }
}
