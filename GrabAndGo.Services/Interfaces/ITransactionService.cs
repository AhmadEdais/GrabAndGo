namespace GrabAndGo.Services.Interfaces
{
    public interface ITransactionService
    {
        Task<List<TransactionListItemDto>> GetUserTransactionsAsync(int userId, int pageNumber, int pageSize);
    }
}
