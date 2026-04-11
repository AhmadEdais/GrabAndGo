using GrabAndGo.DataAccess.Interfaces;
using GrabAndGo.Models.Requests;
using GrabAndGo.Models.Responses;
using GrabAndGo.Services.Interfaces;
using System.Threading.Tasks;

namespace GrabAndGo.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        // Constructor Injection
        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<AuthResponseDto> RegisterUserAsync(UserRegistrationDto dto)
        {
            // 1. Hash the password securely
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.PasswordHash);

            // 2. Overwrite the plain-text password with the hash!
            // This ensures your SqlExecutor sends the HASH to SQL Server, not the real password.
            dto.PasswordHash = hashedPassword;

            // 3. Pass the safe data to the Database layer
            int newUserId = await _userRepository.RegisterUserAsync(dto);

            // 4. INTERCEPT THE DUPLICATE EMAIL FLAG
            if (newUserId == -1)
            {
                // Throw a specific C# exception so the Controller knows exactly what went wrong
                throw new InvalidOperationException("Email is already registered.");
            }

            // 5. Map the data back to the clean Response DTO (only happens if ID > 0)
            return new AuthResponseDto
            {
                UserId = newUserId,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                Token = null
            };
        }

        public async Task<AuthResponseDto?> GetUserByIdAsync(int userId)
        {
            // For simply getting a user, the Service layer acts as a pass-through
            // to the Repository. Later, you could add business logic here 
            // (e.g., checking if the user is suspended before returning their profile).
            return await _userRepository.GetUserByIdAsync(userId);
        }
    }
}