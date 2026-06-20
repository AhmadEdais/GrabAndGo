namespace GrabAndGo.DataAccess.Interfaces
{
    public interface ITransactionRepository
    {
        Task<List<TransactionListItemDto>?> GetUserTransactionsAsync(int userId, int pageNumber, int pageSize);
        Task<bool> DoesUserOwnTransactionAsync(int transactionId, int userId);
    }
}
