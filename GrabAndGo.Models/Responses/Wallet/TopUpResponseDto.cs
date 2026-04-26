namespace GrabAndGo.Models.DTOs
{
    public class TopUpResponseDto
    {
        public int WalletId { get; set; }
        public decimal NewBalance { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}