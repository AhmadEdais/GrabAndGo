namespace GrabAndGo.Models.Responses.Gate;

public class GateQrResponseDto
{
    public int GateQrTokenId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string QrCodeData { get; set; } = string.Empty;
}
