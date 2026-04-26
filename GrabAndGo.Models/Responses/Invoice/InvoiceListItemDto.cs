namespace GrabAndGo.Models.Responses.Invoice
{
    public class InvoiceListItemDto
    {
        public int InvoiceId { get; set; }
        public int TransactionId { get; set; }
        public string? PdfUrlOrPath { get; set; }
        public DateTime GeneratedAt { get; set; }
        public decimal Total { get; set; }
        public string StoreName { get; set; } = null!;
        public string PaymentStatus { get; set; } = null!;

        // Computed: lets the Flutter UI render "Ready" vs "Generating" without a second call.
        // Read-only property → JsonSerializer includes it in output but ignores it on deserialization.
        public bool IsReady => !string.IsNullOrEmpty(PdfUrlOrPath);
    }
}
