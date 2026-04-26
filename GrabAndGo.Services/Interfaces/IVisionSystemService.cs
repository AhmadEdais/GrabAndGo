namespace GrabAndGo.Services.Interfaces
{
    public interface IVisionSystemService
    {
            Task<BindTrackResponseDto?> BindTrackAsync(BindTrackRequestDto request);
    }
}
