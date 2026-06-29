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

    public async Task<GateTokenVerificationDto?> GetGateTokenForVerificationAsync(int gateQrTokenId)
    {
        return await _executor.ExecuteReaderAsync<GateTokenVerificationDto>(
            "SP_GetGateTokenForVerification",
            new { GateQrTokenId = gateQrTokenId }
        );
    }
}
