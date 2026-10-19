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
    string? sort, int? categoryId, string? budget, bool saleOnly = false)
        {
            search = search?.Trim();
            if (search?.Length > 100) search = search[..100];
            budget = budget is "under5" or "5to10" or "10plus" ? budget : null;

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

            if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId.Value);
            if (saleOnly) query = query.Where(p => p.SalePrice.HasValue && p.SalePrice.Value > 0 && p.SalePrice.Value < p.Price);
            query = budget switch
            {
                "under5" => query.Where(p => (p.SalePrice.HasValue && p.SalePrice.Value > 0 && p.SalePrice.Value < p.Price ? p.SalePrice.Value : p.Price) < 5000000m),
                "5to10" => query.Where(p => (p.SalePrice.HasValue && p.SalePrice.Value > 0 && p.SalePrice.Value < p.Price ? p.SalePrice.Value : p.Price) >= 5000000m && (p.SalePrice.HasValue && p.SalePrice.Value > 0 && p.SalePrice.Value < p.Price ? p.SalePrice.Value : p.Price) < 10000000m),
                "10plus" => query.Where(p => (p.SalePrice.HasValue && p.SalePrice.Value > 0 && p.SalePrice.Value < p.Price ? p.SalePrice.Value : p.Price) >= 10000000m),
                _ => query
            };

            query = sort switch
            {
                "price-asc" => query
                    .OrderBy(p => (p.SalePrice.HasValue && p.SalePrice.Value > 0 && p.SalePrice.Value < p.Price ? p.SalePrice.Value : p.Price))
                    .ThenBy(p => p.Id),

                "price-desc" => query
                    .OrderByDescending(p => (p.SalePrice.HasValue && p.SalePrice.Value > 0 && p.SalePrice.Value < p.Price ? p.SalePrice.Value : p.Price))
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

            ViewBag.CategoryOptions = new SelectList(await _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(), "Id", "Name", categoryId);
            ViewBag.BudgetOptions = new SelectList(new[] {
                new { Value = "under5", Text = "Dưới 5 triệu" },
                new { Value = "5to10", Text = "5 đến dưới 10 triệu" },
                new { Value = "10plus", Text = "Từ 10 triệu" }
            }, "Value", "Text", budget);
            ViewData["Search"] = search;
            ViewData["SaleOnly"] = saleOnly;
            ViewData["DiscountCount"] = await _db.Products.CountAsync(p => p.IsActive && p.SalePrice.HasValue && p.SalePrice.Value > 0 && p.SalePrice.Value < p.Price);

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