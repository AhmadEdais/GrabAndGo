using GrabAndGo.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrabAndGo.DataAccess.Interfaces
{
    public interface ISessionRepository
    {
        Task<QrTokenResponseDto?> GenerateSecureTokenAsync(int userId, int storeId, string tokenHash);
        Task<TokenVerificationDto?> GetTokenForVerificationAsync(int tokenId);
        Task<GateEntryResponseDto?> ProcessEntryAsync(int tokenId, int userId, int storeId);
    }
}
