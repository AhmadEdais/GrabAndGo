using GrabAndGo.Application.Interfaces;
using GrabAndGo.Application.DTOs.Vision;
using GrabAndGo.DataAccess.Core;

namespace GrabAndGo.Infrastructure.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly SqlExecutor _sqlExecutor;

        public CartRepository(SqlExecutor sqlExecutor)
        {
            _sqlExecutor = sqlExecutor;
        }

        public async Task<CartSignalRDto?> ProcessVisionEventAsync(VisionEventRequestDto requestDto)
        {
            return await _sqlExecutor.ExecuteNonQueryAsync<CartSignalRDto?>(
                "SP_ProcessVisionEvent",
                requestDto
            );
        }
    }
}