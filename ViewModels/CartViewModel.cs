namespace CCMobile.ViewModels
{
    public class CartViewModel
    {
        public List<CartLineViewModel> Items { get; set; } = new();
        public decimal Total => Items.Sum(i => i.Subtotal);
        public int TotalQuantity => Items.Sum(i => i.Quantity);
        public string? Notice { get; set; }
    }

    public class CartLineViewModel
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int MaxQuantity { get; set; }
        public decimal Subtotal => Price * Quantity;
    }
}
