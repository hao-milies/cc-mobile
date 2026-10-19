using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using CCMobile.Data;
using CCMobile.Models;
using CCMobile.Services;
using CCMobile.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CCMobile.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly ShopDbContext _db;
    private readonly IDataProtector _protector;
    public OrdersController(ShopDbContext db, IDataProtectionProvider protection)
    {
        _db = db;
        _protector = protection.CreateProtector("CCMobile.Checkout.v1");
    }
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index()
    {
        var orders = await _db.Orders.AsNoTracking()
            .Where(x => x.UserId == UserId)
            .OrderByDescending(x => x.CreatedAt).ToListAsync();
        return View(orders);
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _db.Orders.AsNoTracking().Include(x => x.Items).Include(x => x.CancellationRequests)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId);
        return order == null ? NotFound() : View(order);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestCancellation(int id, string? reason)
    {
        var order = await _db.Orders.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId);
        if (order == null) return NotFound();
        reason = reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 1000)
        {
            TempData["OrderError"] = "Vui lòng nhập lý do hủy từ 1 đến 1000 ký tự.";
            return RedirectToAction(nameof(Details), new { id });
        }
        if (!OrderWorkflow.CanRequestCancellation(order.Status))
        {
            TempData["OrderError"] = "Chỉ gửi yêu cầu hủy khi đơn chưa giao và chưa có yêu cầu đang chờ.";
            return RedirectToAction(nameof(Details), new { id });
        }
        await using var transaction = await _db.Database.BeginTransactionAsync();
        var changed = await _db.Orders.Where(x => x.Id == id && x.UserId == UserId && x.Status == order.Status)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, "CancellationRequested"));
        if (changed != 1)
        {
            await transaction.RollbackAsync();
            TempData["OrderError"] = "Trạng thái đơn vừa thay đổi. Vui lòng kiểm tra lại.";
            return RedirectToAction(nameof(Details), new { id });
        }
        _db.OrderCancellationRequests.Add(new OrderCancellationRequest
        {
            OrderId = id, Reason = reason, PreviousOrderStatus = order.Status
        });
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        TempData["OrderSuccess"] = "Đã gửi yêu cầu hủy. Đơn chờ cửa hàng xét duyệt; chưa hoàn kho.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        await HttpContext.Session.LoadAsync();
        var cart = SessionCart.Read(HttpContext.Session);
        var ids = cart.Keys.ToList();
        var products = await _db.Products.AsNoTracking().Where(x => ids.Contains(x.Id)).ToListAsync();
        if (SessionCart.Normalize(cart, products))
        {
            SessionCart.Write(HttpContext.Session, cart);
            return BackToCart("Tồn kho hoặc trạng thái sản phẩm đã thay đổi. Vui lòng kiểm tra lại giỏ hàng.");
        }
        if (cart.Count == 0 || cart.Count > SessionCart.MaxLines)
            return BackToCart("Vui lòng thêm sản phẩm vào giỏ trước khi đặt hàng.");
        var lines = products.OrderBy(x => x.Id).Select(x => new CheckoutLine
        {
            ProductId = x.Id, Name = x.Name, Price = x.EffectivePrice, Quantity = cart[x.Id]
        }).ToList();
        var ticket = new CheckoutTicket
        {
            Key = Guid.NewGuid(), UserId = UserId,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30), Lines = lines
        };
        var user = await _db.Users.AsNoTracking().FirstAsync(x => x.Id == UserId);
        return View(new CheckoutViewModel
        {
            Ticket = _protector.Protect(JsonSerializer.Serialize(ticket)),
            RecipientName = user.FullName, Lines = lines
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutViewModel model)
    {
        CheckoutTicket? ticket;
        try { ticket = JsonSerializer.Deserialize<CheckoutTicket>(_protector.Unprotect(model.Ticket ?? "")); }
        catch (Exception ex) when (ex is CryptographicException or JsonException or ArgumentException)
        { return BackToCart("Phiên đặt hàng không hợp lệ. Vui lòng thử lại."); }
        if (ticket == null || ticket.UserId != UserId)
            return BackToCart("Phiên đặt hàng không hợp lệ.");

        // Một lần xác nhận chỉ tạo một đơn, kể cả khi gửi lại cùng biểu mẫu.
        var existing = await _db.Orders.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CheckoutKey == ticket.Key && x.UserId == UserId);
        if (existing != null) return RedirectToAction(nameof(Details), new { id = existing.Id });
        if (ticket.ExpiresAt < DateTime.UtcNow)
            return BackToCart("Phiên đặt hàng đã hết hạn. Vui lòng xác nhận lại.");

        await HttpContext.Session.LoadAsync();
        var cart = SessionCart.Read(HttpContext.Session);
        if (cart.Count != ticket.Lines.Count || ticket.Lines.Any(x =>
            !cart.TryGetValue(x.ProductId, out var qty) || qty != x.Quantity))
            return BackToCart("Giỏ hàng đã thay đổi. Vui lòng kiểm tra và đặt lại.");

        model.Lines = ticket.Lines;
        if (!ModelState.IsValid) return View(model);

        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var ids = ticket.Lines.Select(x => x.ProductId).ToList();
            var products = await _db.Products.AsNoTracking()
                .Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
            var order = new Order
            {
                CheckoutKey = ticket.Key, UserId = UserId,
                RecipientName = model.RecipientName.Trim(),
                PhoneNumber = model.PhoneNumber.Trim(),
                ShippingAddress = model.ShippingAddress.Trim(),
                Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim()
            };
            // Cùng thứ tự khóa giữa các đơn; trừ kho bằng điều kiện SQL để tránh bán vượt tồn.
            foreach (var line in ticket.Lines.OrderBy(x => x.ProductId))
            {
                if (!products.TryGetValue(line.ProductId, out var product) ||
                    !product.IsActive || product.EffectivePrice != line.Price || line.Quantity < 1 ||
                    line.Quantity > SessionCart.MaxQuantity || product.Price <= 0)
                {
                    await transaction.RollbackAsync();
                    return BackToCart("Sản phẩm hoặc giá bán đã thay đổi. Vui lòng kiểm tra lại giỏ.");
                }
                var affected = await _db.Products.Where(x => x.Id == line.ProductId &&
                    x.IsActive && x.Price == product.Price && x.SalePrice == product.SalePrice && x.StockQuantity >= line.Quantity)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(
                        x => x.StockQuantity, x => x.StockQuantity - line.Quantity));
                if (affected != 1)
                {
                    await transaction.RollbackAsync();
                    return BackToCart("Không đủ tồn kho hoặc giá đã thay đổi. Chưa tạo đơn; vui lòng kiểm tra lại giỏ.");
                }
                order.Items.Add(new OrderItem
                {
                    ProductId = product.Id, Sku = product.Sku, ProductName = product.Name,
                    UnitPrice = product.EffectivePrice, Quantity = line.Quantity
                });
            }
            order.Total = order.Items.Sum(x => x.Subtotal);
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            SessionCart.Write(HttpContext.Session, new());
            TempData["OrderSuccess"] = "Đặt hàng thành công! Đơn đang chờ cửa hàng xác nhận.";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && (sql.Number == 2601 || sql.Number == 2627))
        {
            await transaction.RollbackAsync();
            var saved = await _db.Orders.AsNoTracking()
                .FirstOrDefaultAsync(x => x.CheckoutKey == ticket.Key && x.UserId == UserId);
            if (saved == null) throw;
            return RedirectToAction(nameof(Details), new { id = saved.Id });
        }
        catch (SqlException ex) when (ex.Number == 1205)
        {
            return BackToCart("Có đơn khác đang cập nhật tồn kho. Vui lòng thử đặt lại.");
        }
    }

    private IActionResult BackToCart(string message)
    {
        TempData["CartError"] = message;
        return RedirectToAction("Index", "Cart");
    }
}
