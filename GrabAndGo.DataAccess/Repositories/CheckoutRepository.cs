namespace GrabAndGo.Repositories
{
    public class CheckoutRepository : ICheckoutRepository
    {
        private readonly SqlExecutor _sqlExecutor;

        public CheckoutRepository(SqlExecutor sqlExecutor)
        {
            _sqlExecutor = sqlExecutor;
        }

        public async Task<CheckoutVisionResponseDto?> ProcessCheckoutAsync(CheckoutVisionRequestDto request)
        {
            return await _sqlExecutor.ExecuteNonQueryAsync<CheckoutVisionResponseDto>(
                "SP_ProcessCheckout",
                request
            );
        }
    }
}