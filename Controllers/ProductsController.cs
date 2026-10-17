using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CCMobile.Data;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CCMobile.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ShopDbContext _db;

        public ProductsController(ShopDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(
    string? search,
    int? brandId,
    string? sort)
        {
            search = search?.Trim();

            // Chỉ nhận các cách sắp xếp được hỗ trợ.
            sort = sort is "price-asc" or "price-desc"
                ? sort
                : "newest";

            var query = _db.Products
                .AsNoTracking()
                .Where(p => p.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    p.Sku.Contains(search));
            }

            if (brandId.HasValue)
            {
                query = query.Where(p => p.BrandId == brandId.Value);
            }

            query = sort switch
            {
                "price-asc" => query
                    .OrderBy(p => p.Price)
                    .ThenBy(p => p.Id),

                "price-desc" => query
                    .OrderByDescending(p => p.Price)
                    .ThenBy(p => p.Id),

                _ => query
                    .OrderByDescending(p => p.CreatedAt)
                    .ThenByDescending(p => p.Id)
            };

            var products = await query
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .ToListAsync();

            var brands = await _db.Brands
                .AsNoTracking()
                .OrderBy(b => b.Name)
                .ToListAsync();

            ViewBag.BrandOptions = new SelectList(
                brands, "Id", "Name", brandId);

            ViewBag.SortOptions = new SelectList(
                new[]
                {
            new { Value = "newest", Text = "Mới nhất" },
            new { Value = "price-asc", Text = "Giá tăng dần" },
            new { Value = "price-desc", Text = "Giá giảm dần" }
                },
                "Value",
                "Text",
                sort);

            ViewData["Search"] = search;

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