using GrabAndGo.Models.DTOs;

namespace GrabAndGo.Services.Interfaces
{
    public interface ISessionService
    {
        Task<QrTokenResponseDto?> GenerateSecureTokenAsync(int userId, int storeId);
        Task<TokenVerificationDto?> GetTokenForVerificationAsync(string qrCodeData);
        Task<GateEntryResponseDto?> ProcessStoreEntryAsync(string qrCodeData);
    }
}
