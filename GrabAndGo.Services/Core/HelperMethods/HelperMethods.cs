using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace GrabAndGo.Services.Core.HelperMethods
{
    public class HelperMethods
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger _logger;
        public HelperMethods(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger logger)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }
        

        public string ComputeHmac(string rawPayload)
        {
            string expectedHash;
            string secretKey = _config["QrSecurityKey"] ?? "FallbackSuperSecretKeyForDev2026";
            byte[] keyBytes = Encoding.UTF8.GetBytes(secretKey);
            using (var hmac = new HMACSHA256(keyBytes))
            {
                byte[] payloadBytes = Encoding.UTF8.GetBytes(rawPayload);
                byte[] hashBytes = hmac.ComputeHash(payloadBytes);
                expectedHash = Convert.ToHexString(hashBytes);
            }
            return expectedHash;
        }

        public async Task NotifyVisionSystemAsync(int sessionId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("VisionSystem");
                var response = await client.PostAsJsonAsync("vision/session/assign", new
                {
                    sessionId = sessionId.ToString(),
                    source = "Camera_01"
                });
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning(
                        "Vision system rejected session assignment for {SessionId}: {Status} — {Body}",
                        sessionId, response.StatusCode, body);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to notify vision system for session {SessionId}", sessionId);
            }
        }
    }
}

