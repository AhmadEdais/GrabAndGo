namespace GrabAndGo.DataAccess.Repositories
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly SqlExecutor _executor;

        public InvoiceRepository(SqlExecutor executor)
        {
            _executor = executor;
        }

        public async Task<InvoiceDataDto?> GetInvoiceDataAsync(int transactionId)
        {
            return await _executor.ExecuteReaderAsync<InvoiceDataDto>(
                "SP_GetInvoiceData",
                new { TransactionId = transactionId }
            );
        }

        public async Task<UpdateInvoicePathResponseDto?> UpdateInvoicePathAsync(UpdateInvoicePathRequestDto request)
        {
            return await _executor.ExecuteNonQueryAsync<UpdateInvoicePathResponseDto>(
                "SP_UpdateInvoicePath",
                request
            );
        }

        public async Task<List<PendingInvoiceDto>?> GetPendingInvoicesAsync(int batchSize)
        {
            return await _executor.ExecuteReaderAsync<List<PendingInvoiceDto>>(
                "SP_GetPendingInvoices",
                new { BatchSize = batchSize }
            );
        }

        public async Task<List<InvoiceListItemDto>?> GetUserInvoicesAsync(int userId, int pageNumber, int pageSize)
        {
            return await _executor.ExecuteReaderAsync<List<InvoiceListItemDto>>(
                "SP_GetUserInvoices",
                new { UserId = userId, PageNumber = pageNumber, PageSize = pageSize }
            );
        }
    }
}