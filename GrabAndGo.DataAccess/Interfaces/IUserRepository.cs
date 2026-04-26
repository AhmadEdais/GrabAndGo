namespace GrabAndGo.DataAccess.Interfaces
{
    public interface IUserRepository
    {
        // Executes the JSON POST and returns the new ID
        Task<int> RegisterUserAsync(UserRegistrationDto userDto);

        // Executes the JSON GET and returns the user details
        Task<AuthResponseDto?> GetUserByIdAsync(int userId);
        Task<UserAuthLookupDto?> GetUserAuthByEmailAsync(string email);
    }
}