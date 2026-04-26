namespace GrabAndGo.Models.Responses.Transaction
{
    public class TransactionListItemDto
    {
        public int TransactionId { get; set; }
        public int StoreId { get; set; }
        public string StoreName { get; set; } = null!;
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        public string PaymentStatus { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int ItemCount { get; set; }
    }
}
