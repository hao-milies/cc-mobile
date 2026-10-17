using CCMobile.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CCMobile.Models;
using CCMobile.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
namespace CCMobile.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminProductsController : Controller
    {
        private readonly ShopDbContext _db;

        public AdminProductsController(ShopDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _db.Products
                .AsNoTracking()
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .OrderByDescending(p => p.CreatedAt)
                .ThenByDescending(p => p.Id)
                .ToListAsync();

            return View(products);
        }
        private async Task LoadOptionsAsync(ProductFormViewModel model)
        {
            model.CategoryOptions = await _db.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToListAsync();

            model.BrandOptions = await _db.Brands
                .AsNoTracking()
                .OrderBy(b => b.Name)
                .Select(b => new SelectListItem
                {
                    Value = b.Id.ToString(),
                    Text = b.Name
                })
                .ToListAsync();
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new ProductFormViewModel();

            await LoadOptionsAsync(model);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductFormViewModel model)
        {
            var sku = model.Sku?.Trim() ?? string.Empty;
            var name = model.Name?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(sku))
            {
                ModelState.AddModelError(
                    nameof(model.Sku), "Vui lòng nhập mã sản phẩm.");
            }
            else if (await _db.Products.AnyAsync(p => p.Sku == sku))
            {
                ModelState.AddModelError(
                    nameof(model.Sku), "Mã sản phẩm đã tồn tại.");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError(
                    nameof(model.Name), "Vui lòng nhập tên sản phẩm.");
            }

            if (model.CategoryId.HasValue &&
                !await _db.Categories.AnyAsync(
                    c => c.Id == model.CategoryId.Value))
            {
                ModelState.AddModelError(
                    nameof(model.CategoryId), "Danh mục không tồn tại.");
            }

            if (model.BrandId.HasValue &&
                !await _db.Brands.AnyAsync(
                    b => b.Id == model.BrandId.Value))
            {
                ModelState.AddModelError(
                    nameof(model.BrandId), "Thương hiệu không tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                await LoadOptionsAsync(model);
                return View(model);
            }

            var product = new Product
            {
                Sku = sku,
                Name = name,
                Price = model.Price!.Value,
                StockQuantity = model.StockQuantity!.Value,
                ShortDescription = model.ShortDescription?.Trim(),
                Description = model.Description?.Trim(),
                Color = model.Color?.Trim(),
                WarrantyMonths = model.WarrantyMonths!.Value,
                IsActive = model.IsActive,
                CategoryId = model.CategoryId!.Value,
                BrandId = model.BrandId!.Value,
                CreatedAt = DateTime.UtcNow
            };

            _db.Products.Add(product);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is SqlException sql &&
                      (sql.Number == 2601 || sql.Number == 2627))
            {
                // Xử lý khi hai yêu cầu cùng tạo một mã sản phẩm.
                ModelState.AddModelError(
                    nameof(model.Sku),
                    "Mã sản phẩm đã được sử dụng. Vui lòng chọn mã khác.");

                await LoadOptionsAsync(model);
                return View(model);
            }

            TempData["SuccessMessage"] = "Đã thêm sản phẩm thành công.";

            return RedirectToAction(nameof(Index));
        }
    }
}