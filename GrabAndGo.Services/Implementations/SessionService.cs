namespace GrabAndGo.Services.Implementations;

public class SessionService : ISessionService
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IGateRepository _gateRepository;
    private readonly IConfiguration _config;
    private readonly IGateQrTokenRefreshService _gateQrTokenRefreshService;
    private readonly IBroadcastSessionToDemo _broadcastSessionToDemo;

    private readonly HelperMethods _helperMethods;

    public SessionService(ISessionRepository sessionRepository, IGateRepository gateRepository, IConfiguration config
        ,HelperMethods helperMethods, IGateQrTokenRefreshService gateQrTokenRefreshService, IBroadcastSessionToDemo broadcastSessionToDemo)
    {
        _sessionRepository = sessionRepository;
        _gateRepository = gateRepository;
        _config = config;
        _gateQrTokenRefreshService = gateQrTokenRefreshService;
        _broadcastSessionToDemo = broadcastSessionToDemo;
        _helperMethods = helperMethods;
    }

    public async Task<QrTokenResponseDto?> GenerateSecureTokenAsync(int userId, int storeId)
    {
        string nonce = Guid.NewGuid().ToString("N");

        
        string rawPayload = $"{userId}:{storeId}:{nonce}";

        
        string tokenHash = _helperMethods.ComputeHmac(rawPayload);

        QrTokenResponseDto? dbResponse = await _sessionRepository.GenerateSecureTokenAsync(userId, storeId, tokenHash);

        if (dbResponse is null)
            return null;
        
        dbResponse.QrCodeData = $"{dbResponse.TokenId}|{rawPayload}";

        return dbResponse;
    }

    public async Task<TokenVerificationDto?> GetTokenForVerificationAsync(string qrCodeData)
    {
        // Split the QR code data into TokenId and the rest of the payload
        string[] parts = qrCodeData.Split('|');
        if (parts.Length != 2 || !int.TryParse(parts[0], out int tokenId))
            return null; // Invalid QR code format
        TokenVerificationDto? tokenData = await _sessionRepository.GetTokenForVerificationAsync(tokenId);
        if (tokenData is null) return null;


        string rawPayload = parts[1];

        string expectedHash = _helperMethods.ComputeHmac(rawPayload);

        if (!CryptographicOperations.FixedTimeEquals(
         Encoding.UTF8.GetBytes(expectedHash),
         Encoding.UTF8.GetBytes(tokenData.TokenHash)))
            return null;

        if (tokenData.ConsumedAt != null || tokenData.ExpiresAt < DateTime.UtcNow)
            return null;

        return tokenData;
    }

    public async Task<GateEntryResponseDto?> ProcessStoreEntryAsync(string qrCodeData)
    {
        // PHASE A: The Security Check
        // We call the method you wrote earlier to verify the hash, expiration, and reuse.
        var validToken = await GetTokenForVerificationAsync(qrCodeData);

        if (validToken == null)
        {
            // Token is invalid, tampered with, expired, or already used.
            // The gate stays closed.
            throw new UnauthorizedAccessException("Invalid, expired, or consumed QR Token.");
        }

        // PHASE B: The Database Transaction
        // If we reach here, the token is 100% authentic and ready to burn.
        var entryResult = await _sessionRepository.ProcessEntryAsync(
            validToken.TokenId,
            validToken.UserId,
            validToken.StoreId
        );
        int sessionId = entryResult.SessionId;
        await _helperMethods.NotifyVisionSystemAsync(sessionId);
        return entryResult;
    }
    public async Task<GateEntryResponseDto?> EnterStoreAsync(int userId, string gateToken)
    {
        var parts = gateToken.Split('|');
        if (parts.Length != 2 || !int.TryParse(parts[0], out int gateQrTokenId))
            throw new UnauthorizedAccessException("Malformed gate token.");

        string rawPayload = parts[1];

        var tokenRecord = await _gateRepository.GetGateTokenForVerificationAsync(gateQrTokenId);
        if (tokenRecord == null || tokenRecord.ExpiresAt < DateTime.Now || tokenRecord.ConsumedAt != null)
            throw new UnauthorizedAccessException("Gate token invalid, expired, or already used.");

        string recomputedHash = _helperMethods.ComputeHmac(rawPayload);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(recomputedHash),
                Encoding.UTF8.GetBytes(tokenRecord.TokenHash)))
            throw new UnauthorizedAccessException("Gate token signature invalid.");

        var result = await _sessionRepository.ProcessGateEntryAsync(
            gateQrTokenId, userId, tokenRecord.StoreId)
            ?? throw new UnauthorizedAccessException("Failed to process gate entry.");
        await _gateQrTokenRefreshService.RefreshQrTokenAsync(tokenRecord.StoreId);
        await _helperMethods.NotifyVisionSystemAsync(result.SessionId);
        await _broadcastSessionToDemo.BroadcastSessionEnteredAsync(result.SessionId, tokenRecord.StoreId, DateTime.UtcNow);
        return result;
    }

    public async Task<ActiveSessionDto?> GetUserActiveSessionAsync(int userId)
    {
        return await _sessionRepository.GetUserActiveSessionAsync(userId);
    }

    public async Task<bool> DoesUserOwnActiveSessionAsync(int userId, int sessionId)
    {
        return await _sessionRepository.DoesUserOwnActiveSessionAsync(userId, sessionId);
    }
}