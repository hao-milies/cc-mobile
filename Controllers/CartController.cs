using CCMobile.Data;
using CCMobile.Services;
using CCMobile.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CCMobile.Controllers
{
    public class CartController : Controller
    {
        private readonly ShopDbContext _db;
        public CartController(ShopDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            await HttpContext.Session.LoadAsync();
            var cart = SessionCart.Read(HttpContext.Session);
            var ids = cart.Keys.ToList();
            var products = await _db.Products.AsNoTracking()
                .Include(p => p.Images)
                .Where(p => ids.Contains(p.Id)).ToListAsync();
            var changed = SessionCart.Normalize(cart, products);
            SessionCart.Write(HttpContext.Session, cart);
            var byId = products.ToDictionary(p => p.Id);
            var model = new CartViewModel
            {
                Notice = changed
                    ? "Giỏ hàng đã được cập nhật theo tình trạng bán và tồn kho hiện tại. Vui lòng kiểm tra lại."
                    : null,
                Items = cart.Select(entry =>
                {
                    var product = byId[entry.Key];
                    return new CartLineViewModel
                    {
                        ProductId = product.Id,
                        Name = product.Name,
                        Price = product.EffectivePrice,
                        OriginalPrice = product.Price,
                        Quantity = entry.Value,
                        MaxQuantity = Math.Min(SessionCart.MaxQuantity, product.StockQuantity),
                        ImageUrl = product.Images.OrderByDescending(i => i.IsPrimary)
                            .ThenBy(i => i.DisplayOrder).ThenBy(i => i.Id)
                            .Select(i => i.ImageUrl).FirstOrDefault()
                    };
                }).ToList()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int quantity = 1)
        {
            if (!ModelState.IsValid || quantity < 1 || quantity > SessionCart.MaxQuantity)
                return CartError("Số lượng phải là số nguyên từ 1 đến 99.");

            var product = await _db.Products.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null || !product.IsActive || product.StockQuantity <= 0 || product.Price <= 0)
                return CartError("Sản phẩm hiện không thể thêm vào giỏ hàng.");

            await HttpContext.Session.LoadAsync();
            var cart = SessionCart.Read(HttpContext.Session);
            if (!cart.ContainsKey(productId) && cart.Count >= SessionCart.MaxLines)
                return CartError("Giỏ hàng tối đa 50 loại sản phẩm. Hãy bỏ bớt sản phẩm trước khi thêm.");

            var current = cart.GetValueOrDefault(productId);
            var total = (long)current + quantity;
            if (total > Math.Min(product.StockQuantity, SessionCart.MaxQuantity))
                return CartError("Số lượng trong giỏ vượt tồn kho hoặc giới hạn 99 sản phẩm mỗi loại.");

            cart[productId] = (int)total;
            SessionCart.Write(HttpContext.Session, cart);
            TempData["CartSuccess"] = "Đã thêm sản phẩm vào giỏ hàng.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int productId, int quantity)
        {
            if (!ModelState.IsValid || quantity < 1 || quantity > SessionCart.MaxQuantity)
                return CartError("Số lượng phải là số nguyên từ 1 đến 99. Dùng nút Xóa nếu không muốn mua.");

            await HttpContext.Session.LoadAsync();
            var cart = SessionCart.Read(HttpContext.Session);
            if (!cart.ContainsKey(productId))
                return CartError("Sản phẩm không còn trong giỏ hàng.");

            var product = await _db.Products.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null || !product.IsActive || product.StockQuantity <= 0 || product.Price <= 0)
            {
                cart.Remove(productId);
                SessionCart.Write(HttpContext.Session, cart);
                return CartError("Sản phẩm đã ngừng bán hoặc hết hàng và được bỏ khỏi giỏ.");
            }
            if (quantity > product.StockQuantity)
                return CartError("Số lượng yêu cầu vượt số lượng còn trong kho.");

            cart[productId] = quantity;
            SessionCart.Write(HttpContext.Session, cart);
            TempData["CartSuccess"] = "Đã cập nhật số lượng.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int productId)
        {
            await HttpContext.Session.LoadAsync();
            var cart = SessionCart.Read(HttpContext.Session);
            cart.Remove(productId);
            SessionCart.Write(HttpContext.Session, cart);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            await HttpContext.Session.LoadAsync();
            SessionCart.Write(HttpContext.Session, new());
            return RedirectToAction(nameof(Index));
        }

        private IActionResult CartError(string message)
        {
            TempData["CartError"] = message;
            return RedirectToAction(nameof(Index));
        }
    }
}
