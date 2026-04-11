using GrabAndGo.DataAccess.Core;
using GrabAndGo.DataAccess.Interfaces;
using GrabAndGo.Models.Requests;
using GrabAndGo.Models.Responses;
using System.Threading.Tasks;

namespace GrabAndGo.DataAccess.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly SqlExecutor _executor;

        // Constructor Injection - No static methods!
        public UserRepository(SqlExecutor executor)
        {
            _executor = executor;
        }

        public async Task<int> RegisterUserAsync(UserRegistrationDto userDto)
        {
            // The executor converts 'userDto' to JSON and sends it to @P_JSON_REQUEST
            var response = await _executor.ExecuteNonQueryAsync<AddUserResponse>("SP_InsertUser", userDto);

            return response?.NewUserId ?? 0;
        }

        public async Task<AuthResponseDto?> GetUserByIdAsync(int userId)
        {
            // Sends the anonymous object as parameters, expects a single JSON object back
            return await _executor.ExecuteReaderAsync<AuthResponseDto>("sp_GetUserById_JSON", new { UserId = userId });
        }
    }

    // A private helper class just to catch the JSON response from the SP
    public class AddUserResponse
    {
        public int NewUserId { get; set; }
    }
}