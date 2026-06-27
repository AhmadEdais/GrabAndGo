using GrabAndGo.Models.Responses.Gate;

namespace GrabAndGo.DataAccess.Repositories;

public class GateRepository : IGateRepository
{
    private readonly SqlExecutor _executor;
    public GateRepository(SqlExecutor executor)
    {
        _executor = executor;
    }

    public async Task<GateQrResponseDto?> GenerateGateTokenAsync(int storeId, string tokenHash)
    {
        return await _executor.ExecuteNonQueryAsync<GateQrResponseDto>(
            "SP_GenerateGateToken",
            new { StoreId = storeId, TokenHash = tokenHash }
        );
    }
}
