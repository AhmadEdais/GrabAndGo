namespace GrabAndGo.DataAccess.Repositories
{
    public class WalletRepository : IWalletRepository
    {
        private readonly SqlExecutor _executor;

        public WalletRepository(SqlExecutor executor)
        {
            _executor = executor;
        }

        public async Task<TopUpResponseDto?> TopUpWalletAsync(int userId, decimal amount)
        {
            return await _executor.ExecuteNonQueryAsync<TopUpResponseDto>(
                "SP_TopUpWallet",
                new { UserId = userId, Amount = amount }
            );
        }
        public async Task<WalletBalanceResponseDto?> GetBalanceAsync(int userId)
        {
            return await _executor.ExecuteReaderAsync<WalletBalanceResponseDto>(
                "SP_GetWalletBalance",
                new { UserId = userId }
            );
        }

        public async Task<List<WalletLedgerEntryDto>?> GetUserWalletLedgerAsync(int userId, int pageNumber, int pageSize)
        {
            return await _executor.ExecuteReaderAsync<List<WalletLedgerEntryDto>>(
                "SP_GetUserWalletLedger",
                new { UserId = userId, PageNumber = pageNumber, PageSize = pageSize }
            );
        }
    }
}
