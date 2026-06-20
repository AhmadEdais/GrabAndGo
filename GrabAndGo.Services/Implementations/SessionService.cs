using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace GrabAndGo.Services.Implementations
{
    public class SessionService : ISessionService
    {
        private readonly ISessionRepository _sessionRepository;
        private readonly IConfiguration _config;
        private readonly ILogger _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public SessionService(ISessionRepository sessionRepository, IConfiguration config
            , IHttpClientFactory httpClientFactory, ILogger<SessionService> logger)
        {
            _sessionRepository = sessionRepository;
            _config = config;
            _httpClientFactory = httpClientFactory;
            _logger = logger;

        }

        public async Task<QrTokenResponseDto?> GenerateSecureTokenAsync(int userId, int storeId)
        {
            // 1. Generate a "Nonce" (a unique, random cryptographic string)
            string nonce = Guid.NewGuid().ToString("N");

            // 2. Create the raw payload string. 
            // This is the "Secret" that the QR scanner at the gate will read.
            string rawPayload = $"{userId}:{storeId}:{nonce}";

            // 3. Hash the payload using HMAC-SHA256
            string secretKey = _config["QrSecurityKey"] ?? "FallbackSuperSecretKeyForDev2026";
            byte[] keyBytes = Encoding.UTF8.GetBytes(secretKey);
            string tokenHash;

            using (var hmac = new HMACSHA256(keyBytes))
            {
                byte[] payloadBytes = Encoding.UTF8.GetBytes(rawPayload);
                byte[] hashBytes = hmac.ComputeHash(payloadBytes);
                tokenHash = Convert.ToHexString(hashBytes); // 64-char Hex string for DB
            }

            // 4. Call the DB (SqlExecutor auto-deserializes it now!)
            QrTokenResponseDto? dbResponse = await _sessionRepository.GenerateSecureTokenAsync(userId, storeId, tokenHash);

            if (dbResponse == null)
                return null;

            // 5. Construct the final object for Flutter
            // The physical QR code will hold "TokenId|UserId:StoreId:Nonce"
            // The gate will read this, separate the TokenId to query the DB, 
            // and hash the rest to prove it matches!
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
            if (tokenData == null) return null;
            string secretKey = _config["QrSecurityKey"] ?? "FallbackSuperSecretKeyForDev2026";
            byte[] keyBytes = Encoding.UTF8.GetBytes(secretKey);
            string rawPayload = parts[1];
            string expectedHash;
            using (var hmac = new HMACSHA256(keyBytes))
            {
                byte[] payloadBytes = Encoding.UTF8.GetBytes(rawPayload);
                byte[] hashBytes = hmac.ComputeHash(payloadBytes);
                expectedHash = Convert.ToHexString(hashBytes);
            }
            if (!string.Equals(expectedHash, tokenData.TokenHash, StringComparison.OrdinalIgnoreCase))
                return null; // Hash mismatch - possible tampering
            if (tokenData.ConsumedAt != null || tokenData.ExpiresAt < DateTime.UtcNow)
                return null; // Token already used or expired
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
            try
            {
                var client = _httpClientFactory.CreateClient("VisionSystem");
                var response = await client.PostAsJsonAsync("vision/session/assign", new
                {
                    sessionId = entryResult.SessionId.ToString(),   
                    source = "Camera_01"
                });

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning(
                        "Vision system rejected session assignment for {SessionId}: {Status} — {Body}",
                        entryResult.SessionId, response.StatusCode, body);
                }

            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to notify vision system of new session {SessionId}", entryResult.SessionId);
            }
            return entryResult;
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
}