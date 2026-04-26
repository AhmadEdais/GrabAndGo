namespace GrabAndGo.DataAccess.Interfaces
{
    public interface IWalletRepository
    {
        Task<TopUpResponseDto?> TopUpWalletAsync(int userId, decimal amount);
        Task<WalletBalanceResponseDto?> GetBalanceAsync(int userId);
        Task<List<WalletLedgerEntryDto>?> GetUserWalletLedgerAsync(int userId, int pageNumber, int pageSize);
    }
}
