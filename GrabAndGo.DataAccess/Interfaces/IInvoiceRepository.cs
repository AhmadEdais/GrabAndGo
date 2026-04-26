namespace GrabAndGo.DataAccess.Interfaces
{
    public interface IInvoiceRepository
    {
        Task<InvoiceDataDto?> GetInvoiceDataAsync(int transactionId);
        Task<UpdateInvoicePathResponseDto?> UpdateInvoicePathAsync(UpdateInvoicePathRequestDto request);
        Task<List<PendingInvoiceDto>?> GetPendingInvoicesAsync(int batchSize);
        Task<List<InvoiceListItemDto>?> GetUserInvoicesAsync(int userId, int pageNumber, int pageSize);
    }
}
