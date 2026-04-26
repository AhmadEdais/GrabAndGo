namespace GrabAndGo.Models.Responses.Invoice
{
    public class InvoiceDataDto
    {
        public int InvoiceId { get; set; }
        public int TransactionId { get; set; }
        public string? PdfUrlOrPath { get; set; }
        public DateTime GeneratedAt { get; set; }

        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public string PaymentStatus { get; set; } = null!;

        public int CustomerUserId { get; set; }
        public string CustomerFirstName { get; set; } = null!;
        public string CustomerLastName { get; set; } = null!;
        public string CustomerEmail { get; set; } = null!;

        public int StoreId { get; set; }
        public string StoreCode { get; set; } = null!;
        public string StoreName { get; set; } = null!;
        public string StoreTimezone { get; set; } = null!;

        public List<InvoiceLineItemDto> Items { get; set; } = new List<InvoiceLineItemDto>();
    }

    public class InvoiceLineItemDto
    {
        public int TransactionItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string SKU { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public decimal VAT_Rate { get; set; }
    }
}
