namespace GrabAndGo.Services.Implementations
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;

        public ProductService(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<List<ProductListItemDto>> GetProductsAsync(int pageNumber, int pageSize, string? search)
        {
            // 1. Clamp pagination — catalog scrolls long, so the upper bound is higher than other lists
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 200) pageSize = 200;

            // 2. Normalize search: empty/whitespace becomes NULL so the SP takes the fast path
            string? normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

            // 3. Fetch — repository returns null for empty result; normalize to empty list for the controller
            var result = await _productRepository.GetProductsAsync(pageNumber, pageSize, normalizedSearch);
            return result ?? new List<ProductListItemDto>();
        }
    }
}
