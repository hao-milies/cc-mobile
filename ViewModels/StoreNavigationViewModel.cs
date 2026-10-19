using CCMobile.Models;
namespace CCMobile.ViewModels;
public class StoreNavigationViewModel
{
    public List<Brand> Brands { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
}
