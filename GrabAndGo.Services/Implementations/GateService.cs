namespace GrabAndGo.Services.Implementations;

public class GateService : IGateService
{
    private readonly IGateRepository _gateRepository;
    private readonly IConfiguration _config;

    public GateService(IGateRepository gateRepository, IConfiguration config)
    {
        _gateRepository = gateRepository;
        _config = config;
    }

    public async Task<GateQrResponseDto?> GenerateGateTokenAsync(int storeId)
    {
        string nonce = Guid.NewGuid().ToString("N");
        string rawPayload = $"{storeId}:{nonce}";
        string secretKey = _config["QrSecurityKey"] ?? "FallbackSuperSecretKeyForDev2026";

        byte[] keyBytes = Encoding.UTF8.GetBytes(secretKey);
        string tokenHash;

        using (var hmac = new HMACSHA256(keyBytes))
        {
            byte[] payloadBytes = Encoding.UTF8.GetBytes(rawPayload);
            byte[] hashBytes = hmac.ComputeHash(payloadBytes);
            tokenHash = Convert.ToHexString(hashBytes);
        }
        var result = await _gateRepository.GenerateGateTokenAsync(storeId, tokenHash);
        if(result == null)
            return null;

        string rawToken = $"{result.GateQrTokenId}|{storeId}:{nonce}";
        string encodedToken = Uri.EscapeDataString(rawToken);
        string qrContent = $"{_config["FrontendBaseUrl"]}/store-detail.html?gateToken={encodedToken}";

        return new GateQrResponseDto
        {
            GateQrTokenId = result.GateQrTokenId,
            ExpiresAt = result.ExpiresAt,
            QrCodeData = qrContent
        };
    }
}