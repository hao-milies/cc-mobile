using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CCMobile.Data;

namespace CCMobile.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ShopDbContext _db;

        public ProductsController(ShopDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _db.Products
                .AsNoTracking()
                .Where(p => p.IsActive)
                .Include(p => p.Brand)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(products);
        }
    }
}