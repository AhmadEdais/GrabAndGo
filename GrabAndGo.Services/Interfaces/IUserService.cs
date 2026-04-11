using GrabAndGo.Models.Requests;
using GrabAndGo.Models.Responses;
using System.Threading.Tasks;

namespace GrabAndGo.Services.Interfaces
{
    public interface IUserService
    {
        Task<AuthResponseDto> RegisterUserAsync(UserRegistrationDto dto);

        Task<AuthResponseDto?> GetUserByIdAsync(int userId);
    }
}