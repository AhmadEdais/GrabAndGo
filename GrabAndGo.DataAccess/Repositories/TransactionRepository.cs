namespace GrabAndGo.DataAccess.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly SqlExecutor _executor;

        public TransactionRepository(SqlExecutor executor)
        {
            _executor = executor;
        }

        public async Task<List<TransactionListItemDto>?> GetUserTransactionsAsync(int userId, int pageNumber, int pageSize)
        {
            return await _executor.ExecuteReaderAsync<List<TransactionListItemDto>>(
                "SP_GetUserTransactions",
                new { UserId = userId, PageNumber = pageNumber, PageSize = pageSize }
            );
        }
    }
}
