using GrabAndGo.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrabAndGo.Services.Interfaces
{
    public interface IWalletService
    {
        Task<TopUpResponseDto?> TopUpWalletAsync(int userId, decimal amount);
        Task<WalletBalanceResponseDto?> GetWalletBalanceAsync(int userId);
    }
}
