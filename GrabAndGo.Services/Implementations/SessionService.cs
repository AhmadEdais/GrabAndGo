using GrabAndGo.DataAccess.Interfaces;
using GrabAndGo.Models.DTOs;
using GrabAndGo.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GrabAndGo.Services.Implementations
{
    public class SessionService : ISessionService
    {
        private readonly ISessionRepository _sessionRepository;
        private readonly IConfiguration _config;

        public SessionService(ISessionRepository sessionRepository, IConfiguration config)
        {
            _sessionRepository = sessionRepository;
            _config = config;
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

            // 4. Call the DB (Returns JSON like: {"TokenId": 14, "ExpiresAt": "2026-04-11T..."})
            string dbResultJson = await _sessionRepository.GenerateSecureTokenAsync(userId, storeId, tokenHash);

            if (string.IsNullOrEmpty(dbResultJson))
                return null;

            // 5. Parse the DB JSON and construct the final object for Flutter
            var dbResponse = JsonSerializer.Deserialize<QrTokenResponseDto>(dbResultJson);

            if (dbResponse != null)
            {
                // The physical QR code will hold "TokenId|UserId:StoreId:Nonce"
                // The gate will read this, separate the TokenId to query the DB, 
                // and hash the rest to prove it matches!
                dbResponse.QrCodeData = $"{dbResponse.TokenId}|{rawPayload}";
            }

            return dbResponse;
        }
    }
}