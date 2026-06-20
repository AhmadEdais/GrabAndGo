namespace GrabAndGo.Api.Controllers
{
    /// <summary>
    /// Invoice list, status polling, manual generation, and PDF download.
    /// All endpoints check ownership: a user may only see their own invoices.
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class InvoicesController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;
        private readonly ITransactionService _transactionService;
        public InvoicesController(IInvoiceService invoiceService,ITransactionService transactionService)
        {
            _invoiceService = invoiceService;
            _transactionService = transactionService;
        }

        /// <summary>
        /// Get paginated invoices belonging to the authenticated user, most recent first.
        /// Each item exposes an <c>IsReady</c> flag so the UI can skip polling for already-rendered PDFs.
        /// </summary>
        /// <param name="page">1-based page number. Defaults to 1.</param>
        /// <param name="pageSize">Items per page. Defaults to 20, capped at 100.</param>
        /// <response code="200">Returns array of invoice summaries (possibly empty).</response>
        /// <response code="401">JWT missing or invalid.</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<InvoiceListItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyInvoices([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized(new { message = "Invalid token identity." });

            var result = await _invoiceService.GetUserInvoicesAsync(userId, page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Manually trigger PDF generation for a specific invoice. The InvoiceWorker normally does this
        /// automatically; this endpoint is for testing and recovery.
        /// </summary>
        /// <param name="transactionId">Transaction ID whose invoice should be generated.</param>
        /// <response code="200">PDF generated (or already existed). Body includes <c>PdfUrlOrPath</c>.</response>
        /// <response code="400">Generation failed. Body includes a <c>Message</c> explaining why.</response>
        /// <response code="401">JWT missing or invalid.</response>
        /// <response code="403">Caller does not own this invoice.</response>
        /// <response code="404">Invoice not found for the given TransactionId.</response>
        [HttpPost("{transactionId:int}/generate")]
        [ProducesResponseType(typeof(UpdateInvoicePathResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Generate(int transactionId)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized(new { message = "Invalid token identity." });

            var data = await _invoiceService.GetInvoiceDataAsync(transactionId);
            if (data == null) return NotFound(new { message = "Invoice not found." });
            if (data.CustomerUserId != userId) return Forbid();

            var result = await _invoiceService.GenerateInvoiceAsync(transactionId);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Get invoice status. The Flutter app polls this every 2–3 seconds after checkout
        /// until <c>status</c> is <c>ready</c>, then fetches the PDF.
        /// </summary>
        /// <param name="transactionId">Transaction ID whose invoice to check.</param>
        /// <response code="200">PDF is ready. Body includes <c>pdfUrl</c>, <c>total</c>, <c>paymentStatus</c>, <c>generatedAt</c>.</response>
        /// <response code="202">PDF is still being generated. Poll again shortly.</response>
        /// <response code="401">JWT missing or invalid.</response>
        /// <response code="403">Caller does not own this invoice.</response>
        /// <response code="404">Invoice not found.</response>
        [HttpGet("{transactionId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetStatus(int transactionId)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized(new { message = "Invalid token identity." });

            var data = await _invoiceService.GetInvoiceDataAsync(transactionId);
            if (data == null) return NotFound(new { message = "Invoice not found." });
            if (data.CustomerUserId != userId) return Forbid();

            if (string.IsNullOrEmpty(data.PdfUrlOrPath))
            {
                return StatusCode(StatusCodes.Status202Accepted, new
                {
                    status = "generating",
                    transactionId,
                    message = "Invoice PDF is still being generated. Try again shortly."
                });
            }

            return Ok(new
            {
                status = "ready",
                transactionId,
                pdfUrl = Url.Action(nameof(GetPdf), new { transactionId }),
                total = data.Total,
                paymentStatus = data.PaymentStatus,
                generatedAt = data.GeneratedAt
            });
        }

        /// <summary>
        /// Stream the invoice PDF file as <c>application/pdf</c>.
        /// </summary>
        /// <param name="transactionId">Transaction ID whose PDF to download.</param>
        /// <response code="200">Returns the binary PDF.</response>
        /// <response code="401">JWT missing or invalid.</response>
        /// <response code="403">Caller does not own this invoice.</response>
        /// <response code="404">Invoice not found, or PDF not yet generated, or file missing on disk.</response>
        [HttpGet("{transactionId:int}/pdf")]
        [Produces("application/pdf")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPdf(int transactionId)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized(new { message = "Invalid token identity." });

            var data = await _invoiceService.GetInvoiceDataAsync(transactionId);
            if (data == null) return NotFound(new { message = "Invoice not found." });
            if (data.CustomerUserId != userId) return Forbid();

            if (string.IsNullOrEmpty(data.PdfUrlOrPath) || !System.IO.File.Exists(data.PdfUrlOrPath))
            {
                return NotFound(new { message = "Invoice PDF is not ready yet." });
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(data.PdfUrlOrPath);
            return File(fileBytes, "application/pdf", $"invoice-{transactionId}.pdf");
        }
        [HttpGet("GetInvoiceData/{transactionId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetInvoiceData(int transactionId)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized(new { message = "Invalid token identity." });

            var owns = await _transactionService.DoesUserOwnTransactionAsync(userId, transactionId);
            if (!owns) return Forbid();

            var data = await _invoiceService.GetInvoiceDataAsync(transactionId);
            if (data == null) return NotFound(new { message = "Invoice not found." });
            if (data.CustomerUserId != userId) return Forbid();

            return Ok(data);
        }

        private bool TryGetUserId(out int userId)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out userId);
        }
    }
}
