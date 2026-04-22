using GrabAndGo.Models.Requests.Vision_System;
using GrabAndGo.Models.Responses.Vision_System;


namespace GrabAndGo.DataAccess.Interfaces
{
    public interface IVisionSystemRepository
    {
        Task<BindTrackResponseDto?> BindTrackAsync(BindTrackRequestDto request);
    }
}
