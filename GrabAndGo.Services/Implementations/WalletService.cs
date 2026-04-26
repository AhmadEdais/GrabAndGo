namespace GrabAndGo.Services.Implementations
{
    public class WalletService : IWalletService
    {
        private readonly IWalletRepository _walletRepository;
        public WalletService(IWalletRepository walletRepository)
        {
            _walletRepository = walletRepository;
        }
        public async Task<TopUpResponseDto?> TopUpWalletAsync(int userId, decimal amount)
        {
            // You could add extra business logic here if needed (e.g., checking daily top-up limits)
            return await _walletRepository.TopUpWalletAsync(userId, amount);
        }
        public async Task<WalletBalanceResponseDto?> GetWalletBalanceAsync(int userId)
        {
            var balance = await _walletRepository.GetBalanceAsync(userId);

            // Optional: If the user doesn't have a wallet yet, you could throw an exception
            // or return a default DTO with 0 balance.
            if (balance == null)
            {
                throw new KeyNotFoundException("Wallet not found for this user.");
            }

            return balance;
        }

        public async Task<List<WalletLedgerEntryDto>> GetUserWalletLedgerAsync(int userId, int pageNumber, int pageSize)
        {
            // Clamp pagination to safe bounds before hitting the SP
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var result = await _walletRepository.GetUserWalletLedgerAsync(userId, pageNumber, pageSize);
            return result ?? new List<WalletLedgerEntryDto>();
        }
    }
}
