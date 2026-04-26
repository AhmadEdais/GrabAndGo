namespace GrabAndGo.DataAccess.Repositories
{
    public class SessionRepository : ISessionRepository
    {
        private readonly SqlExecutor _executor;

        public SessionRepository(SqlExecutor executor)
        {
            _executor = executor;
        }

        public async Task<QrTokenResponseDto?> GenerateSecureTokenAsync(int userId, int storeId, string tokenHash)
        {
            // FIX: Use ExecuteNonQueryAsync to utilize the JSON Output Parameter
            return await _executor.ExecuteNonQueryAsync<QrTokenResponseDto>(
                "SP_GenerateEntryQrToken",
                new { UserId = userId, StoreId = storeId, TokenHash = tokenHash }
            );
        }
        public async Task<TokenVerificationDto?> GetTokenForVerificationAsync(int tokenId)
        {
            // Executes the GET SP and automatically deserializes the single JSON object
            return await _executor.ExecuteReaderAsync<TokenVerificationDto>(
                "SP_GetTokenForVerification",
                new { TokenId = tokenId }
            );
        }
        public async Task<GateEntryResponseDto?> ProcessEntryAsync(int tokenId, int userId, int storeId)
        {
            // The SqlExecutor will auto-serialize the anonymous object,
            // run the transaction, and deserialize the output JSON into our Response DTO!
            return await _executor.ExecuteNonQueryAsync<GateEntryResponseDto>(
                "SP_ProcessStoreEntry",
                new { TokenId = tokenId, UserId = userId, StoreId = storeId }
            );
        }

        public async Task<ActiveSessionDto?> GetUserActiveSessionAsync(int userId)
        {
            return await _executor.ExecuteReaderAsync<ActiveSessionDto>(
                "SP_GetUserActiveSession",
                new { UserId = userId }
            );
        }

        public async Task<bool> DoesUserOwnActiveSessionAsync(int userId, int sessionId)
        {
            // Scalar SP returns 1 if the user owns an active session with that ID, NULL otherwise.
            // ExecuteScalarAsync<int> returns 0 when the SP returns no rows.
            var result = await _executor.ExecuteScalarAsync<int>(
                "SP_DoesUserOwnActiveSession",
                new { UserId = userId, SessionId = sessionId }
            );
            return result == 1;
        }
    }
}