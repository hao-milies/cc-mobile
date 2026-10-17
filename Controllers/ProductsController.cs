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
                .Include(p => p.Images)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(products);
        }
        public async Task<IActionResult> Details(int id)
        {
            var product = await _db.Products
                .AsNoTracking()
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.PhoneSpecification)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
    }
}