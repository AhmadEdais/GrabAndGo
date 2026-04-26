namespace GrabAndGo.DataAccess.Interfaces
{
    public interface ISessionRepository
    {
        Task<QrTokenResponseDto?> GenerateSecureTokenAsync(int userId, int storeId, string tokenHash);
        Task<TokenVerificationDto?> GetTokenForVerificationAsync(int tokenId);
        Task<GateEntryResponseDto?> ProcessEntryAsync(int tokenId, int userId, int storeId);
        Task<ActiveSessionDto?> GetUserActiveSessionAsync(int userId);
        Task<bool> DoesUserOwnActiveSessionAsync(int userId, int sessionId);
    }
}
