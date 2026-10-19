using CCMobile.Models;
namespace CCMobile.ViewModels;

public class AdminOrderListViewModel
{
    public List<Order> Orders { get; set; } = new();
    public string? Status { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
}
