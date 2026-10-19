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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetVisibility(int id, bool? expectedActive)
        {
            if (!ModelState.IsValid || !expectedActive.HasValue)
            {
                TempData["VisibilityError"] = "Yêu cầu không hợp lệ. Vui lòng tải lại trang.";
                return RedirectToAction(nameof(Index));
            }
            var desired = !expectedActive.Value;
            // Chỉ đổi trạng thái hiển thị; không ghi đè giá, tồn kho hay nội dung.
            var changed = await _db.Products
                .Where(p => p.Id == id && p.IsActive == expectedActive.Value)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, desired));
            if (changed == 0)
            {
                TempData["VisibilityError"] = "Sản phẩm đã đổi trạng thái hoặc không còn tồn tại. Danh sách đã được tải lại.";
            }
            else
            {
                TempData["SuccessMessage"] = desired
                    ? "Đã hiện bán lại sản phẩm. Tồn kho được giữ nguyên."
                    : "Đã ngừng bán sản phẩm. Thông tin và lịch sử đơn hàng được giữ nguyên.";
            }
            return RedirectToAction(nameof(Index));
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

            ValidatePhoneSpecification(model);

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
                SalePrice = model.SalePrice,
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

            ApplyPhoneSpecification(product, model);
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
        [RequestSizeLimit(22 * 1024 * 1024)]
        public async Task<IActionResult> UploadImage(
            int id, ProductImageUploadViewModel model)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();

            model.ProductId = product.Id;
            model.ProductName = product.Name;

            if (model.Images.Count == 0 || model.Images.Count > 10)
                ModelState.AddModelError(nameof(model.Images),
                    "Vui lòng chọn từ 1 đến 10 ảnh mỗi lần.");

            var validatedFiles = new List<(IFormFile File, string Extension)>();
            if (model.Images.Count is >= 1 and <= 10)
            {
                foreach (var file in model.Images)
                {
                    if (file.Length == 0 || file.Length > 2 * 1024 * 1024)
                    {
                        ModelState.AddModelError(nameof(model.Images),
                            $"Ảnh {file.FileName} phải lớn hơn 0 và không quá 2 MB.");
                        continue;
                    }
                    var extension = await GetImageExtensionAsync(file);
                    if (extension == null)
                    {
                        ModelState.AddModelError(nameof(model.Images),
                            $"File {file.FileName} không có định dạng JPEG hoặc PNG.");
                        continue;
                    }
                    validatedFiles.Add((file, extension));
                }
            }

            if (!ModelState.IsValid) return View(model);

            var directory = Path.Combine(_environment.WebRootPath, "images", "products");
            Directory.CreateDirectory(directory);
            var existingImages = await _db.ProductImages
                .Where(i => i.ProductId == product.Id).ToListAsync();
            var nextOrder = existingImages.Count == 0
                ? 0 : existingImages.Max(i => i.DisplayOrder) + 1;
            var hasPrimary = existingImages.Any(i => i.IsPrimary);
            var createdPaths = new List<string>();

            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                foreach (var item in validatedFiles)
                {
                    var fileName = $"{Guid.NewGuid():N}{item.Extension}";
                    var filePath = Path.Combine(directory, fileName);
                    await using (var output = new FileStream(filePath, FileMode.CreateNew))
                    {
                        createdPaths.Add(filePath);
                        await item.File.CopyToAsync(output);
                    }
                    _db.ProductImages.Add(new ProductImage
                    {
                        ProductId = product.Id,
                        ImageUrl = $"/images/products/{fileName}",
                        AltText = product.Name,
                        DisplayOrder = nextOrder++,
                        IsPrimary = !hasPrimary
                    });
                    hasPrimary = true;
                }
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                foreach (var path in createdPaths)
                {
                    if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                }
                throw;
            }

            TempData["SuccessMessage"] =
                $"Đã thêm {validatedFiles.Count} ảnh cho sản phẩm {product.Name}.";
            return RedirectToAction(nameof(Images), new { id = product.Id });
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
                .Include(p => p.PhoneSpecification)
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
                SalePrice = product.SalePrice,
                StockQuantity = product.StockQuantity,
                ShortDescription = product.ShortDescription,
                Description = product.Description,
                Color = product.Color,
                WarrantyMonths = product.WarrantyMonths,
                IsActive = product.IsActive,
                CategoryId = product.CategoryId,
                BrandId = product.BrandId
            };

            FillPhoneSpecification(model, product.PhoneSpecification);
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
                .Include(p => p.PhoneSpecification)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            ViewData["ProductId"] = product.Id;

            await ValidateProductAsync(model, product.Id);
            ValidatePhoneSpecification(model);

            if (!ModelState.IsValid)
            {
                await LoadOptionsAsync(model);
                return View(model);
            }

            product.Sku = model.Sku.Trim();
            product.Name = model.Name.Trim();
            product.Price = model.Price!.Value;
            product.SalePrice = model.SalePrice;
            product.StockQuantity = model.StockQuantity!.Value;
            product.ShortDescription = model.ShortDescription?.Trim();
            product.Description = model.Description?.Trim();
            product.Color = model.Color?.Trim();
            product.WarrantyMonths = model.WarrantyMonths!.Value;
            product.IsActive = model.IsActive;
            product.CategoryId = model.CategoryId!.Value;
            product.BrandId = model.BrandId!.Value;
            ApplyPhoneSpecification(product, model);

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


        [HttpGet]
        public async Task<IActionResult> Images(int id)
        {
            var product = await _db.Products.AsNoTracking()
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPrimaryImage(int id, int imageId)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);
            var images = await _db.ProductImages
                .Where(i => i.ProductId == id).ToListAsync();
            var selected = images.FirstOrDefault(i => i.Id == imageId);
            if (selected == null) return NotFound();

            // Lưu việc bỏ ảnh chính trước để không vi phạm chỉ mục duy nhất.
            foreach (var image in images) image.IsPrimary = false;
            await _db.SaveChangesAsync();
            selected.IsPrimary = true;
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] = "Đã cập nhật ảnh đại diện.";
            return RedirectToAction(nameof(Images), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int id, int imageId)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);
            var images = await _db.ProductImages
                .Where(i => i.ProductId == id).ToListAsync();
            var selected = images.FirstOrDefault(i => i.Id == imageId);
            if (selected == null) return NotFound();

            var wasPrimary = selected.IsPrimary;
            _db.ProductImages.Remove(selected);
            await _db.SaveChangesAsync();

            if (wasPrimary)
            {
                var replacement = images.Where(i => i.Id != imageId)
                    .OrderBy(i => i.DisplayOrder).ThenBy(i => i.Id)
                    .FirstOrDefault();
                if (replacement != null)
                {
                    replacement.IsPrimary = true;
                    await _db.SaveChangesAsync();
                }
            }

            await transaction.CommitAsync();
            // Chỉ gỡ bản ghi; giữ file để không ảnh hưởng nơi khác dùng cùng đường dẫn.
            TempData["SuccessMessage"] = "Đã gỡ ảnh khỏi sản phẩm.";
            return RedirectToAction(nameof(Images), new { id });
        }


        private void ValidatePhoneSpecification(ProductFormViewModel model)
        {
            if (!model.HasPhoneSpecification)
            {
                // Bỏ qua cả lỗi binding của các ô thông số không áp dụng.
                foreach (var key in ModelState.Keys
                    .Where(k => k.StartsWith("Phone", StringComparison.Ordinal)).ToList())
                    ModelState.Remove(key);
                return;
            }

            if (string.IsNullOrWhiteSpace(model.PhoneChipset))
                ModelState.AddModelError(nameof(model.PhoneChipset), "Vui lòng nhập chip xử lý.");

            if (!model.PhoneRamGb.HasValue)
                ModelState.AddModelError(nameof(model.PhoneRamGb), "Vui lòng nhập RAM.");
            if (!model.PhoneStorageGb.HasValue)
                ModelState.AddModelError(nameof(model.PhoneStorageGb), "Vui lòng nhập bộ nhớ.");
            if (!model.PhoneScreenSizeInches.HasValue)
                ModelState.AddModelError(nameof(model.PhoneScreenSizeInches), "Vui lòng nhập kích thước màn hình.");
        }

        private void ApplyPhoneSpecification(Product product, ProductFormViewModel model)
        {
            if (!model.HasPhoneSpecification)
            {
                if (product.PhoneSpecification != null)
                {
                    _db.Remove(product.PhoneSpecification);
                    product.PhoneSpecification = null;
                }
                return;
            }

            var specs = product.PhoneSpecification ?? new PhoneSpecification();
            specs.Chipset = model.PhoneChipset!.Trim();
            specs.RamGb = model.PhoneRamGb!.Value;
            specs.StorageGb = model.PhoneStorageGb!.Value;
            specs.ScreenSizeInches = model.PhoneScreenSizeInches!.Value;
            specs.ScreenResolution = model.PhoneScreenResolution?.Trim();
            specs.PanelType = model.PhonePanelType?.Trim();
            specs.RefreshRateHz = model.PhoneRefreshRateHz;
            specs.RearCamera = model.PhoneRearCamera?.Trim();
            specs.FrontCamera = model.PhoneFrontCamera?.Trim();
            specs.BatteryMah = model.PhoneBatteryMah;
            specs.ChargingWatts = model.PhoneChargingWatts;
            specs.OperatingSystem = model.PhoneOperatingSystem?.Trim();
            specs.WeightGrams = model.PhoneWeightGrams;
            product.PhoneSpecification = specs;
        }

        private static void FillPhoneSpecification(
            ProductFormViewModel model, PhoneSpecification? specs)
        {
            model.HasPhoneSpecification = specs != null;
            if (specs == null) return;
            model.PhoneChipset = specs.Chipset;
            model.PhoneRamGb = specs.RamGb;
            model.PhoneStorageGb = specs.StorageGb;
            model.PhoneScreenSizeInches = specs.ScreenSizeInches;
            model.PhoneScreenResolution = specs.ScreenResolution;
            model.PhonePanelType = specs.PanelType;
            model.PhoneRefreshRateHz = specs.RefreshRateHz;
            model.PhoneRearCamera = specs.RearCamera;
            model.PhoneFrontCamera = specs.FrontCamera;
            model.PhoneBatteryMah = specs.BatteryMah;
            model.PhoneChargingWatts = specs.ChargingWatts;
            model.PhoneOperatingSystem = specs.OperatingSystem;
            model.PhoneWeightGrams = specs.WeightGrams;
        }

        private static async Task<string?> GetImageExtensionAsync(
            IFormFile file)
        {
            var header = new byte[8];

            await using var stream = file.OpenReadStream();

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
                return ".jpg";
            }

            if (total == 8 &&
                header.SequenceEqual(new byte[]
                {
            0x89, 0x50, 0x4E, 0x47,
            0x0D, 0x0A, 0x1A, 0x0A
                }))
            {
                return ".png";
            }

            return null;
        }
    }
}