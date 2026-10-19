using System.ComponentModel.DataAnnotations;
namespace CCMobile.Models;

public class OrderCancellationRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    [MaxLength(1000)] public string Reason { get; set; } = "";
    [MaxLength(30)] public string PreviousOrderStatus { get; set; } = "";
    [MaxLength(20)] public string Decision { get; set; } = "Pending";
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(1000)] public string? Response { get; set; }
    [MaxLength(450)] public string? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
}
