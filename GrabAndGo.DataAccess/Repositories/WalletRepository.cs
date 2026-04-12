using GrabAndGo.DataAccess.Core;
using GrabAndGo.DataAccess.Interfaces;
using GrabAndGo.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
