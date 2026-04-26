namespace GrabAndGo.Services.Interfaces
{
    public interface IInvoiceService
    {
        Task<UpdateInvoicePathResponseDto> GenerateInvoiceAsync(int transactionId);
        Task<InvoiceDataDto?> GetInvoiceDataAsync(int transactionId);
        Task<List<PendingInvoiceDto>?> GetPendingInvoicesAsync(int batchSize);
        Task<List<InvoiceListItemDto>> GetUserInvoicesAsync(int userId, int pageNumber, int pageSize);
    }
}
