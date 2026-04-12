using GrabAndGo.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrabAndGo.DataAccess.Interfaces
{
    public interface IWalletRepository
    {
        Task<TopUpResponseDto?> TopUpWalletAsync(int userId, decimal amount);
        Task<WalletBalanceResponseDto?> GetBalanceAsync(int userId);
    }
}
