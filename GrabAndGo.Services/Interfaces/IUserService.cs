namespace GrabAndGo.Services.Interfaces
{
    public interface IUserService
    {
        Task<AuthResponseDto> RegisterUserAsync(UserRegistrationDto dto);

        Task<AuthResponseDto?> GetUserByIdAsync(int userId);
        Task<AuthResponseDto> LoginUserAsync(LoginRequestDto dto);
    }
}