namespace GrabAndGo.Services.Interfaces
{
    public interface IProductService
    {
        Task<List<ProductListItemDto>> GetProductsAsync(int pageNumber, int pageSize, string? search);
    }
}
