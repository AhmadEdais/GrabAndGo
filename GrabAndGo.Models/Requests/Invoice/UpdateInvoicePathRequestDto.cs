namespace GrabAndGo.Models.Requests.Invoice
{
    public class UpdateInvoicePathRequestDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "TransactionId must be a positive integer.")]
        public int TransactionId { get; set; }

        [Required]
        [MaxLength(500)]
        public string PdfUrlOrPath { get; set; } = null!;
    }
}
