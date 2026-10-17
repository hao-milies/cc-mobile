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
        private readonly IWebHostEnvironment _environment;

        public AdminProductsController(ShopDbContext db, IWebHostEnvironment environment)
        {
            _db = db;
            _environment = environment;
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
        [HttpGet]
        public async Task<IActionResult> UploadImage(int id)
        {
            var product = await _db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(new ProductImageUploadViewModel
            {
                ProductId = product.Id,
                ProductName = product.Name
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(3 * 1024 * 1024)]
        public async Task<IActionResult> UploadImage(
            int id,
            ProductImageUploadViewModel model)
        {
            var product = await _db.Products
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            model.ProductId = product.Id;
            model.ProductName = product.Name;

            var file = model.Image;
            string? extension = null;

            if (file != null)
            {
                if (file.Length == 0 || file.Length > 2 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        nameof(model.Image),
                        "Ảnh phải có dung lượng lớn hơn 0 và không quá 2 MB.");
                }
                else
                {
                    // Kiểm tra dấu hiệu định dạng trong nội dung file.
                    // Không chỉ tin vào tên file hoặc ContentType gửi từ trình duyệt.
                    var header = new byte[8];

                    await using (var stream = file.OpenReadStream())
                    {
                        var total = 0;

                        while (total < header.Length)
                        {
                            var read = await stream.ReadAsync(
                                header.AsMemory(total, header.Length - total));

                            if (read == 0)
                            {
                                break;
                            }

                            total += read;
                        }

                        if (total >= 3 &&
                            header[0] == 0xFF &&
                            header[1] == 0xD8 &&
                            header[2] == 0xFF)
                        {
                            extension = ".jpg";
                        }
                        else if (total == 8 &&
                            header.SequenceEqual(new byte[]
                            {
                        0x89, 0x50, 0x4E, 0x47,
                        0x0D, 0x0A, 0x1A, 0x0A
                            }))
                        {
                            extension = ".png";
                        }
                    }

                    if (extension == null)
                    {
                        ModelState.AddModelError(
                            nameof(model.Image),
                            "Chỉ nhận file ảnh JPEG hoặc PNG.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var directory = Path.Combine(
                _environment.WebRootPath, "images", "products");

            Directory.CreateDirectory(directory);

            // Tự sinh tên, không dùng tên file do người dùng gửi.
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(directory, fileName);

            var images = await _db.ProductImages
                .Where(i => i.ProductId == product.Id)
                .ToListAsync();

            var productImage = new ProductImage
            {
                ProductId = product.Id,
                ImageUrl = $"/images/products/{fileName}",
                AltText = string.IsNullOrWhiteSpace(model.AltText)
                    ? product.Name
                    : model.AltText.Trim(),
                DisplayOrder = images.Count == 0
                    ? 0
                    : images.Max(i => i.DisplayOrder) + 1,
                IsPrimary = !images.Any(i => i.IsPrimary)
            };

            try
            {
                await using (var output = new FileStream(
                    filePath, FileMode.CreateNew))
                {
                    await file!.CopyToAsync(output);
                }

                _db.ProductImages.Add(productImage);
                await _db.SaveChangesAsync();
            }
            catch
            {
                // Dọn file vừa tạo nếu không lưu thành công vào database.
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                throw;
            }

            TempData["SuccessMessage"] =
                $"Đã thêm ảnh cho sản phẩm {product.Name}.";

            return RedirectToAction(nameof(Index));
        }
        private async Task ValidateProductAsync(
    ProductFormViewModel model,
    int productId)
        {
            var sku = model.Sku?.Trim() ?? string.Empty;
            var name = model.Name?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(sku))
            {
                ModelState.AddModelError(
                    nameof(model.Sku), "Vui lòng nhập mã sản phẩm.");
            }
            else if (await _db.Products.AnyAsync(
                p => p.Sku == sku && p.Id != productId))
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
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            var model = new ProductFormViewModel
            {
                Sku = product.Sku,
                Name = product.Name,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                ShortDescription = product.ShortDescription,
                Description = product.Description,
                Color = product.Color,
                WarrantyMonths = product.WarrantyMonths,
                IsActive = product.IsActive,
                CategoryId = product.CategoryId,
                BrandId = product.BrandId
            };

            ViewData["ProductId"] = product.Id;

            await LoadOptionsAsync(model);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            ProductFormViewModel model)
        {
            var product = await _db.Products
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            ViewData["ProductId"] = product.Id;

            await ValidateProductAsync(model, product.Id);

            if (!ModelState.IsValid)
            {
                await LoadOptionsAsync(model);
                return View(model);
            }

            product.Sku = model.Sku.Trim();
            product.Name = model.Name.Trim();
            product.Price = model.Price!.Value;
            product.StockQuantity = model.StockQuantity!.Value;
            product.ShortDescription = model.ShortDescription?.Trim();
            product.Description = model.Description?.Trim();
            product.Color = model.Color?.Trim();
            product.WarrantyMonths = model.WarrantyMonths!.Value;
            product.IsActive = model.IsActive;
            product.CategoryId = model.CategoryId!.Value;
            product.BrandId = model.BrandId!.Value;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is SqlException sql &&
                      (sql.Number == 2601 || sql.Number == 2627))
            {
                ModelState.AddModelError(
                    nameof(model.Sku),
                    "Mã sản phẩm đã được sử dụng. Vui lòng chọn mã khác.");

                await LoadOptionsAsync(model);
                return View(model);
            }

            TempData["SuccessMessage"] =
                "Đã cập nhật sản phẩm thành công.";

            return RedirectToAction(nameof(Index));
        }
    }
}