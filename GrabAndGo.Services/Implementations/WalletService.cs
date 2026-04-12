using GrabAndGo.DataAccess.Interfaces;
using GrabAndGo.Models.DTOs;
using GrabAndGo.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
