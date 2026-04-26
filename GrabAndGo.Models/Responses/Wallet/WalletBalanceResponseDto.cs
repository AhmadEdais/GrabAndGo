namespace GrabAndGo.Models.DTOs
{
    public class WalletBalanceResponseDto
    {
        public int WalletId { get; set; }
        public decimal CurrentBalance { get; set; }
        public DateTime LastUpdatedAt { get; set; }
    }
}