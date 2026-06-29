namespace GrabAndGo.Services.Implementations;

public class GateService : IGateService
{
    private readonly IGateRepository _gateRepository;
    private readonly IConfiguration _config;
    private readonly HelperMethods _helperMethods;

    public GateService(IGateRepository gateRepository, IConfiguration config, HelperMethods helperMethods)
    {
        _gateRepository = gateRepository;
        _config = config;
        _helperMethods = helperMethods;
    }

    public async Task<GateQrResponseDto?> GenerateGateTokenAsync(int storeId)
    {
        string nonce = Guid.NewGuid().ToString("N");
        string rawPayload = $"{storeId}:{nonce}";
       
        string tokenHash = _helperMethods.ComputeHmac(rawPayload);

        var result = await _gateRepository.GenerateGateTokenAsync(storeId, tokenHash);
        if(result is null)
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