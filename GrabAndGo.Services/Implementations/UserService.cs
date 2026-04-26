namespace GrabAndGo.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _config; // Needed for JWT Secret
        // Constructor Injection
        public UserService(IUserRepository userRepository, IConfiguration config)
        {
            _userRepository = userRepository;
            _config = config;
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
        public async Task<AuthResponseDto> LoginUserAsync(LoginRequestDto dto)
        {
            // 1. Ask DB for the user profile + password hash
            var internalUser = await _userRepository.GetUserAuthByEmailAsync(dto.Email);

            // 2. If null, the email doesn't exist. Fail generic.
            if (internalUser == null)
            {
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            // 3. Let BCrypt mathematically verify the plain-text against the salt/hash
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, internalUser.PasswordHash);

            if (!isPasswordValid)
            {
                // Always use the exact same error message to prevent email enumeration attacks
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            // 4. Generate the JWT Token
            string token = GenerateJwtToken(internalUser);

            // 5. Map safe data to the final Response DTO
            return new AuthResponseDto
            {
                UserId = internalUser.UserId,
                FirstName = internalUser.FirstName,
                LastName = internalUser.LastName,
                Email = internalUser.Email,
                Token = token
            };
        }

        // Private helper to mint the JWT
        private string GenerateJwtToken(UserAuthLookupDto user)
        {
            // Get the secret key from appsettings.json
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["GRABANDGO_JWT_KEY"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Create the payload (claims)
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()), // Subject is the UserId
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("FirstName", user.FirstName), // Custom claim
                new Claim("LastName", user.LastName),   // Custom claim
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // Unique Token ID
            };

            // Assemble the token (valid for 7 days)
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
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