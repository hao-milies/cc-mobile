using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CCMobile.Models;

public class Order
{
    public int Id { get; set; }
    public Guid CheckoutKey { get; set; }
    [MaxLength(450)] public string UserId { get; set; } = "";
    public ApplicationUser User { get; set; } = null!;
    [MaxLength(100)] public string RecipientName { get; set; } = "";
    [MaxLength(20)] public string PhoneNumber { get; set; } = "";
    [MaxLength(500)] public string ShippingAddress { get; set; } = "";
    [MaxLength(1000)] public string? Note { get; set; }
    [MaxLength(30)] public string Status { get; set; } = "Pending";
    [MaxLength(20)] public string PaymentMethod { get; set; } = "COD";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column(TypeName = "decimal(28,2)")] public decimal Total { get; set; }
    public List<OrderCancellationRequest> CancellationRequests { get; set; } = new();
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    [MaxLength(50)] public string Sku { get; set; } = "";
    [MaxLength(200)] public string ProductName { get; set; } = "";
    [Column(TypeName = "decimal(18,2)")] public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    [NotMapped] public decimal Subtotal => UnitPrice * Quantity;
}
