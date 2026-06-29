using GrabAndGo.Models.Responses.QrTokens;
namespace GrabAndGo.DataAccess.Interfaces;

public interface IGateRepository
{
    Task<GateQrResponseDto?> GenerateGateTokenAsync(int storeId, string tokenHash);
    Task<GateTokenVerificationDto?> GetGateTokenForVerificationAsync(int gateQrTokenId);
}
