using CCMobile.Data;
using CCMobile.Models;
using CCMobile.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CCMobile.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminBrandsController : Controller
    {
        private readonly ShopDbContext _db;
        public AdminBrandsController(ShopDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var items = await _db.Brands.AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new LookupListItemViewModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    ProductCount = _db.Products.Count(p => p.BrandId == x.Id)
                }).ToListAsync();
            return View(items);
        }

        [HttpGet]
        public IActionResult Create()
        {
            PrepareForm(false);
            return View("Form", new LookupFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LookupFormViewModel model)
        {
            await ValidateNameAsync(model, null);
            if (!ModelState.IsValid)
            {
                PrepareForm(false);
                return View("Form", model);
            }
            _db.Brands.Add(new Brand
            {
                Name = model.Name.Trim(),
                Description = model.Description?.Trim()
            });
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã thêm thương hiệu.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _db.Brands.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (item == null) return NotFound();
            PrepareForm(true, id);
            return View("Form", new LookupFormViewModel
            {
                Name = item.Name,
                Description = item.Description
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LookupFormViewModel model)
        {
            var item = await _db.Brands.FirstOrDefaultAsync(x => x.Id == id);
            if (item == null) return NotFound();
            await ValidateNameAsync(model, id);
            if (!ModelState.IsValid)
            {
                PrepareForm(true, id);
                return View("Form", model);
            }
            item.Name = model.Name.Trim();
            item.Description = model.Description?.Trim();
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã cập nhật thương hiệu.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.Brands.FirstOrDefaultAsync(x => x.Id == id);
            if (item == null) return NotFound();
            // Tính cả sản phẩm đang ẩn.
            if (await _db.Products.AnyAsync(p => p.BrandId == id))
            {
                TempData["ErrorMessage"] =
                    "Không thể xóa thương hiệu đang được sản phẩm sử dụng. "
                    + "Hãy chuyển các sản phẩm sang thương hiệu khác trước.";
                return RedirectToAction(nameof(Index));
            }
            _db.Brands.Remove(item);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is SqlException sql && sql.Number == 547)
            {
                TempData["ErrorMessage"] =
                    "Thương hiệu vừa được sử dụng bởi dữ liệu khác nên không thể xóa.";
                return RedirectToAction(nameof(Index));
            }
            TempData["SuccessMessage"] = "Đã xóa thương hiệu.";
            return RedirectToAction(nameof(Index));
        }

        private async Task ValidateNameAsync(LookupFormViewModel model, int? id)
        {
            var name = model.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError(nameof(model.Name), "Vui lòng nhập tên thương hiệu.");
                return;
            }
            if (await _db.Brands.AnyAsync(x => x.Name == name && (!id.HasValue || x.Id != id.Value)))
                ModelState.AddModelError(nameof(model.Name), "Tên thương hiệu đã tồn tại.");
        }

        private void PrepareForm(bool isEdit, int? id = null)
        {
            ViewData["IsEdit"] = isEdit;
            ViewData["ItemId"] = id;
            ViewData["Title"] = isEdit ? "Sửa thương hiệu" : "Thêm thương hiệu";
        }
    }
}
