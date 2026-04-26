namespace GrabAndGo.Application.DTOs.Vision
{
    public class CartSignalRDto
    {
        public int CartId { get; set; }
        public int SessionId { get; set; }
        public int CartVersion { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public decimal WalletBalance { get; set; }
        public decimal CartTotal { get; set; }
        public bool IsShortfall { get; set; } = false;

        public decimal ShortfallAmount { get; set; }

        public List<CartItemSignalRDto> CartItems { get; set; } = new List<CartItemSignalRDto>();
    }

    public class CartItemSignalRDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string AiLabel { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }
}