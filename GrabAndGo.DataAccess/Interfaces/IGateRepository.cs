namespace GrabAndGo.DataAccess.Interfaces;

public interface IGateRepository
{
    Task<GateQrResponseDto?> GenerateGateTokenAsync(int storeId, string tokenHash);
    
}
