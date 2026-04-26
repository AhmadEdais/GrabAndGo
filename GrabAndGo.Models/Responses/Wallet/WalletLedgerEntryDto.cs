namespace GrabAndGo.Models.Responses.Wallet
{
    public class WalletLedgerEntryDto
    {
        public int LedgerEntryId { get; set; }
        public int WalletId { get; set; }
        public int? RelatedTransactionId { get; set; }
        public string EntryType { get; set; } = null!;
        public decimal Amount { get; set; }
        public decimal BalanceAfter { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Reference { get; set; }
    }
}
