using GrabAndGo.DataAccess.Core;
using GrabAndGo.DataAccess.Interfaces;

namespace GrabAndGo.DataAccess.Repositories
{
    internal class SessionRepository : ISessionRepository
    {
        private readonly SqlExecutor _executor;

        public SessionRepository(SqlExecutor executor)
        {
            _executor = executor;
        }

        public async Task<string> GenerateSecureTokenAsync(int userId, int storeId, string tokenHash)
        {
            var token = await _executor.ExecuteScalarAsync<string>(
                "SP_GenerateEntryQrToken",
                new { UserId = userId, StoreId = storeId, TokenHash = tokenHash }
            );

            return token ?? string.Empty;
        }
    }
}