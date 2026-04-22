using GrabAndGo.DataAccess.Core;
using GrabAndGo.DataAccess.Interfaces;
using GrabAndGo.Models.Requests.Vision_System;
using GrabAndGo.Models.Responses.Vision_System;


namespace GrabAndGo.DataAccess.Repositories
{
    public class VisionSystemRepository : IVisionSystemRepository
    {
        private readonly SqlExecutor _executer;
        public VisionSystemRepository(SqlExecutor executor)
        {
            _executer = executor;
        }
        public async Task<BindTrackResponseDto?> BindTrackAsync(BindTrackRequestDto request)
        {
            return await _executer.ExecuteNonQueryAsync<BindTrackResponseDto>(
                "SP_BindSessionTrack",
                request);
        }
    }
}
