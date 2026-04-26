namespace GrabAndGo.DataAccess.Interfaces
{
    public interface IProductRepository
    {
        Task<List<ProductListItemDto>?> GetProductsAsync(int pageNumber, int pageSize, string? search);
    }
}
