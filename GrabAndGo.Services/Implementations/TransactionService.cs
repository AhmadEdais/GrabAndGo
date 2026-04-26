namespace GrabAndGo.Services.Implementations
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _transactionRepository;

        public TransactionService(ITransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        public async Task<List<TransactionListItemDto>> GetUserTransactionsAsync(int userId, int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var result = await _transactionRepository.GetUserTransactionsAsync(userId, pageNumber, pageSize);
            return result ?? new List<TransactionListItemDto>();
        }
    }
}
