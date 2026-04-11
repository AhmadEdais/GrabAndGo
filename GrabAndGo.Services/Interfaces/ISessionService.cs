using GrabAndGo.Models.DTOs;

namespace GrabAndGo.Services.Interfaces
{
    public interface ISessionService
    {
        Task<QrTokenResponseDto?> GenerateSecureTokenAsync(int userId, int storeId);
    }
}
