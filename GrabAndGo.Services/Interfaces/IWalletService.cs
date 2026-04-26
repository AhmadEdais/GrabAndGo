namespace GrabAndGo.Services.Interfaces
{
    public interface IWalletService
    {
        Task<TopUpResponseDto?> TopUpWalletAsync(int userId, decimal amount);
        Task<WalletBalanceResponseDto?> GetWalletBalanceAsync(int userId);
        Task<List<WalletLedgerEntryDto>> GetUserWalletLedgerAsync(int userId, int pageNumber, int pageSize);
    }
}
