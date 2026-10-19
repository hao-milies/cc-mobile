using System.Security.Claims;
using CCMobile.Data;
using CCMobile.Services;
using CCMobile.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CCMobile.Controllers;

[Authorize(Roles = "Admin")]
public class AdminOrdersController : Controller
{
    private readonly ShopDbContext _db;
    public AdminOrdersController(ShopDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? status, string? search, int page = 1)
    {
        status = OrderWorkflow.Statuses.Contains(status ?? "") ? status : null;
        search = search?.Trim();
        if (search?.Length > 100) search = search[..100];
        var query = _db.Orders.AsNoTracking();
        if (status != null) query = query.Where(x => x.Status == status);
        if (!string.IsNullOrEmpty(search))
        {
            int.TryParse(search.TrimStart('#'), out var orderId);
            query = query.Where(x => x.Id == orderId ||
                x.RecipientName.Contains(search) || x.PhoneNumber.Contains(search));
        }
        var count = await query.CountAsync();
        var pages = Math.Max(1, (int)Math.Ceiling(count / 20d));
        page = Math.Clamp(page, 1, pages);
        return View(new AdminOrderListViewModel
        {
            Status = status, Search = search, Page = page, TotalPages = pages, TotalCount = count,
            Orders = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
                .Skip((page - 1) * 20).Take(20).ToListAsync()
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _db.Orders.AsNoTracking().Include(x => x.Items).Include(x => x.CancellationRequests)
            .FirstOrDefaultAsync(x => x.Id == id);
        return order == null ? NotFound() : View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, string expectedStatus, string targetStatus)
    {
        if (!OrderWorkflow.CanMove(expectedStatus, targetStatus))
            return Result(id, "Không thể chuyển trạng thái theo yêu cầu.", false);

        await using var transaction = await _db.Database.BeginTransactionAsync();
        // Chỉ một yêu cầu thắng khi hai quản trị viên thao tác trên cùng trạng thái cũ.
        var changed = await _db.Orders.Where(x => x.Id == id && x.Status == expectedStatus)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, targetStatus));
        if (changed != 1)
        {
            await transaction.RollbackAsync();
            return Result(id, "Đơn đã thay đổi hoặc không tồn tại. Hãy kiểm tra lại trạng thái mới.", false);
        }

        await transaction.CommitAsync();
        return Result(id, $"Đã chuyển đơn sang: {OrderWorkflow.Label(targetStatus)}.", true);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewCancellation(int id, Guid requestId, string? decision, string? response)
    {
        response = response?.Trim();
        if ((decision != "Approve" && decision != "Reject") ||
            string.IsNullOrWhiteSpace(response) || response.Length > 1000)
            return Result(id, "Chọn quyết định hợp lệ và nhập phản hồi từ 1 đến 1000 ký tự.", false);
        bool approve = decision == "Approve";
        var request = await _db.OrderCancellationRequests.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == requestId && x.OrderId == id);
        if (request == null || request.Decision != "Pending" ||
            !OrderWorkflow.CanRequestCancellation(request.PreviousOrderStatus))
            return Result(id, "Yêu cầu không tồn tại hoặc đã được xét duyệt.", false);

        await using var transaction = await _db.Database.BeginTransactionAsync();
        var target = approve ? "Cancelled" : request.PreviousOrderStatus;
        var changed = await _db.Orders.Where(x => x.Id == id && x.Status == "CancellationRequested")
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, target));
        if (changed != 1)
        {
            await transaction.RollbackAsync();
            return Result(id, "Đơn vừa thay đổi. Hãy tải lại trang.", false);
        }
        // Kiểm tra requestId và quyết định cũ để tab cũ không duyệt nhầm yêu cầu mới.
        var reviewed = await _db.OrderCancellationRequests
            .Where(x => x.Id == requestId && x.OrderId == id && x.Decision == "Pending")
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Decision, approve ? "Approved" : "Rejected")
                .SetProperty(x => x.Response, response)
                .SetProperty(x => x.ReviewedAt, (DateTime?)DateTime.UtcNow)
                .SetProperty(x => x.ReviewedByUserId, User.FindFirstValue(ClaimTypes.NameIdentifier)));
        if (reviewed != 1)
        {
            await transaction.RollbackAsync();
            return Result(id, "Yêu cầu đã được xử lý. Không cập nhật hoặc hoàn kho thêm.", false);
        }
        if (approve)
        {
            var quantities = await _db.OrderItems.Where(x => x.OrderId == id)
                .GroupBy(x => x.ProductId)
                .Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => (long)x.Quantity) })
                .OrderBy(x => x.ProductId).ToListAsync();
            foreach (var line in quantities)
            {
                // Không để cộng kho bị tràn số hoặc cập nhật thiếu sản phẩm.
                if (line.Quantity <= 0 || line.Quantity > int.MaxValue)
                {
                    await transaction.RollbackAsync();
                    return Result(id, "Dữ liệu số lượng không hợp lệ. Chưa hủy đơn hoặc hoàn kho.", false);
                }
                int quantity = (int)line.Quantity;
                var restored = await _db.Products
                    .Where(x => x.Id == line.ProductId && x.StockQuantity <= int.MaxValue - quantity)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(
                        x => x.StockQuantity, x => x.StockQuantity + quantity));
                if (restored != 1)
                {
                    await transaction.RollbackAsync();
                    return Result(id, "Không thể hoàn kho. Đơn được giữ nguyên; vui lòng kiểm tra sản phẩm.", false);
                }
            }
        }


        await transaction.CommitAsync();
        return Result(id, approve
            ? "Đã duyệt hủy và hoàn kho một lần."
            : "Đã từ chối yêu cầu hủy. Đơn trở lại trạng thái trước khi yêu cầu.", true);
    }

    private IActionResult Result(int id, string message, bool success)
    {
        TempData[success ? "AdminOrderSuccess" : "AdminOrderError"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }
}
