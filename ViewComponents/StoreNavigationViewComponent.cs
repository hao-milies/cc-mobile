using CCMobile.Data;
using CCMobile.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CCMobile.ViewComponents;
public class StoreNavigationViewComponent : ViewComponent
{
    private readonly ShopDbContext _db;
    public StoreNavigationViewComponent(ShopDbContext db) => _db = db;
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = new StoreNavigationViewModel();
        if (!UserClaimsPrincipal.IsInRole("Admin"))
        {
            model.Brands = await _db.Brands.AsNoTracking()
                .Where(b => _db.Products.Any(p => p.IsActive && p.BrandId == b.Id))
                .OrderBy(b => b.Name).ToListAsync();
            model.Categories = await _db.Categories.AsNoTracking()
                .Where(c => _db.Products.Any(p => p.IsActive && p.CategoryId == c.Id))
                .OrderBy(c => c.Name).ToListAsync();
        }
        return View(model);
    }
}
