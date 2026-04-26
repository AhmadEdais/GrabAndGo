namespace GrabAndGo.Services.Interfaces
{
    public interface ISessionService
    {
        Task<QrTokenResponseDto?> GenerateSecureTokenAsync(int userId, int storeId);
        Task<TokenVerificationDto?> GetTokenForVerificationAsync(string qrCodeData);
        Task<GateEntryResponseDto?> ProcessStoreEntryAsync(string qrCodeData);
        Task<ActiveSessionDto?> GetUserActiveSessionAsync(int userId);
        Task<bool> DoesUserOwnActiveSessionAsync(int userId, int sessionId);
    }
}
