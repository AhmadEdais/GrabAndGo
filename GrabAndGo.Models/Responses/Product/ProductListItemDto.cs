namespace GrabAndGo.Models.Responses.Product
{
    public class ProductListItemDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = null!;
        public string SKU { get; set; } = null!;
        public decimal PriceGross { get; set; }
        public decimal VAT_Rate { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
    }
}
