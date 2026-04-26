namespace GrabAndGo.DataAccess.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly SqlExecutor _executor;

        public ProductRepository(SqlExecutor executor)
        {
            _executor = executor;
        }

        public async Task<List<ProductListItemDto>?> GetProductsAsync(int pageNumber, int pageSize, string? search)
        {
            return await _executor.ExecuteReaderAsync<List<ProductListItemDto>>(
                "SP_GetProducts",
                new { PageNumber = pageNumber, PageSize = pageSize, Search = search }
            );
        }
    }
}
