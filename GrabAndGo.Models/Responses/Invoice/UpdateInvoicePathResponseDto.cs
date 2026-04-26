namespace GrabAndGo.Models.Responses.Invoice
{
    public class UpdateInvoicePathResponseDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = null!;
        public int TransactionId { get; set; }
        public string? PdfUrlOrPath { get; set; }
    }
}
