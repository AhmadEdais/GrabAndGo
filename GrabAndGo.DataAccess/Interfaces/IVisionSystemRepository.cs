namespace GrabAndGo.DataAccess.Interfaces
{
    public interface IVisionSystemRepository
    {
        Task<BindTrackResponseDto?> BindTrackAsync(BindTrackRequestDto request);
    }
}
