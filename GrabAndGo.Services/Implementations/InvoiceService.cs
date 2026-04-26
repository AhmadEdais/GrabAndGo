namespace GrabAndGo.Services.Implementations
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IConfiguration _config;

        private const string DefaultStorageFolder = "invoices";

        public InvoiceService(IInvoiceRepository invoiceRepository, IConfiguration config)
        {
            _invoiceRepository = invoiceRepository;
            _config = config;
        }

        public async Task<UpdateInvoicePathResponseDto> GenerateInvoiceAsync(int transactionId)
        {
            var data = await _invoiceRepository.GetInvoiceDataAsync(transactionId);

            if (data == null)
            {
                return new UpdateInvoicePathResponseDto
                {
                    IsSuccess = false,
                    Message = "Invoice stub not found for the specified transaction.",
                    TransactionId = transactionId,
                    PdfUrlOrPath = null
                };
            }

            if (!string.IsNullOrWhiteSpace(data.PdfUrlOrPath))
            {
                return new UpdateInvoicePathResponseDto
                {
                    IsSuccess = true,
                    Message = "Invoice PDF already exists; returning existing path.",
                    TransactionId = transactionId,
                    PdfUrlOrPath = data.PdfUrlOrPath
                };
            }

            byte[] pdfBytes = RenderPdf(data);

            string folder = _config["Invoice:StorageFolder"] ?? DefaultStorageFolder;
            Directory.CreateDirectory(folder);
            string fileName = $"invoice-{transactionId}.pdf";
            string fullPath = Path.Combine(folder, fileName);
            await File.WriteAllBytesAsync(fullPath, pdfBytes);

            var updateResult = await _invoiceRepository.UpdateInvoicePathAsync(new UpdateInvoicePathRequestDto
            {
                TransactionId = transactionId,
                PdfUrlOrPath = fullPath
            });

            return updateResult ?? new UpdateInvoicePathResponseDto
            {
                IsSuccess = false,
                Message = "Invoice PDF saved to disk but database update returned no response.",
                TransactionId = transactionId,
                PdfUrlOrPath = fullPath
            };
        }

        public async Task<InvoiceDataDto?> GetInvoiceDataAsync(int transactionId)
        {
            return await _invoiceRepository.GetInvoiceDataAsync(transactionId);
        }

        public async Task<List<PendingInvoiceDto>?> GetPendingInvoicesAsync(int batchSize)
        {
            return await _invoiceRepository.GetPendingInvoicesAsync(batchSize);
        }

        public async Task<List<InvoiceListItemDto>> GetUserInvoicesAsync(int userId, int pageNumber, int pageSize)
        {
            // Clamp pagination to safe bounds before hitting the SP
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var result = await _invoiceRepository.GetUserInvoicesAsync(userId, pageNumber, pageSize);
            return result ?? new List<InvoiceListItemDto>();
        }

        // PDF rendering — QuestPDF fluent API. Kept private because it's a pure transform
        // from DTO → bytes with no external dependencies.
        private static byte[] RenderPdf(InvoiceDataDto data)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // Header: big "INVOICE" title + store name
                    page.Header().Column(col =>
                    {
                        col.Item().Text("INVOICE").FontSize(28).Bold().AlignCenter();
                        col.Item().Text(data.StoreName).FontSize(12).AlignCenter();
                    });

                    page.Content().PaddingVertical(15).Column(column =>
                    {
                        // Meta row: invoice details on the left, customer on the right
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Invoice #: {data.TransactionId}").Bold();
                                c.Item().Text($"Date: {data.CreatedAt:yyyy-MM-dd HH:mm} UTC");
                                c.Item().Text($"Status: {data.PaymentStatus}");
                                c.Item().Text($"Store: {data.StoreCode}");
                            });
                            row.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().Text("Bill To").Bold();
                                c.Item().Text($"{data.CustomerFirstName} {data.CustomerLastName}");
                                c.Item().Text(data.CustomerEmail);
                            });
                        });

                        column.Item().PaddingVertical(15).LineHorizontal(1);

                        // Line items table
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);    // Product name
                                columns.RelativeColumn(2);    // SKU
                                columns.RelativeColumn(1);    // Qty
                                columns.RelativeColumn(1.5f); // Unit price
                                columns.RelativeColumn(1.5f); // Line total
                            });

                            table.Header(header =>
                            {
                                header.Cell().BorderBottom(1).PaddingBottom(5).Text("Product").Bold();
                                header.Cell().BorderBottom(1).PaddingBottom(5).Text("SKU").Bold();
                                header.Cell().BorderBottom(1).PaddingBottom(5).AlignRight().Text("Qty").Bold();
                                header.Cell().BorderBottom(1).PaddingBottom(5).AlignRight().Text("Unit Price").Bold();
                                header.Cell().BorderBottom(1).PaddingBottom(5).AlignRight().Text("Line Total").Bold();
                            });

                            foreach (var item in data.Items)
                            {
                                table.Cell().PaddingVertical(3).Text(item.ProductName);
                                table.Cell().PaddingVertical(3).Text(item.SKU);
                                table.Cell().PaddingVertical(3).AlignRight().Text(item.Quantity.ToString());
                                table.Cell().PaddingVertical(3).AlignRight().Text($"{item.UnitPrice:F2}");
                                table.Cell().PaddingVertical(3).AlignRight().Text($"{item.LineTotal:F2}");
                            }
                        });

                        // Totals block (right-aligned)
                        column.Item().PaddingTop(20).AlignRight().Column(totals =>
                        {
                            totals.Item().Text($"Subtotal: {data.Subtotal:F2} JOD");
                            totals.Item().Text($"Tax: {data.Tax:F2} JOD");
                            totals.Item().PaddingTop(5).Text($"Total: {data.Total:F2} JOD").Bold().FontSize(14);
                        });
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("Thank you for shopping with ").FontSize(9);
                        t.Span("Grab & Go").FontSize(9).Bold();
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
