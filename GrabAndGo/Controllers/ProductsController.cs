namespace GrabAndGo.Api.Controllers
{
    /// <summary>
    /// Catalog browse — active products only, alphabetical, with optional name/SKU search.
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        /// <summary>
        /// Browse the active product catalog, alphabetical by name.
        /// </summary>
        /// <param name="page">1-based page number. Defaults to 1.</param>
        /// <param name="pageSize">Items per page. Defaults to 50, capped at 200.</param>
        /// <param name="search">Optional case-insensitive substring filter against <c>Name</c> or <c>SKU</c>.</param>
        /// <response code="200">Returns array of products (possibly empty).</response>
        /// <response code="401">JWT missing or invalid.</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<ProductListItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetProducts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? search = null)
        {
            var result = await _productService.GetProductsAsync(page, pageSize, search);
            return Ok(result);
        }
    }
}
