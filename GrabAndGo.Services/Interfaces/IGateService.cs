namespace GrabAndGo.Services.Interfaces;

public interface IGateService
{
    Task<GateQrResponseDto?> GenerateGateTokenAsync(int storeId);
}
