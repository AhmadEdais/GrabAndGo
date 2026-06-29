namespace GrabAndGo.Models.Responses.QrTokens;

public class GateTokenVerificationDto
{
    public int GateQrTokenId { get; set; }
    public int StoreId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
}
