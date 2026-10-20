using CCMobile.Localization;
using CCMobile.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CCMobile.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/products")]
public class ProductsApiController(ShopDbContext db) : ControllerBase
{
    // Public catalogue only: no stock counts, user data or inactive products.
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Get([FromQuery] string? search, CancellationToken cancellationToken)
    {
        search = search?.Trim() ?? "";
        if (search.Length > 100)
            return BadRequest(new { message = StoreText.T("Từ khóa tối đa 100 ký tự.") });
        if (search.Length < 2)
            return Ok(new { items = Array.Empty<object>() });

        var items = await db.Products.AsNoTracking()
            .Where(p => p.IsActive && (p.Name.Contains(search) || p.Sku.Contains(search)))
            .OrderBy(p => p.Name.StartsWith(search) ? 0 : 1)
            .ThenBy(p => p.Name).ThenBy(p => p.Id)
            .Take(6)
            .Select(p => new
            {
                id = p.Id,
                name = p.Name,
                brand = p.Brand.Name,
                price = p.SalePrice.HasValue && p.SalePrice.Value > 0 && p.SalePrice.Value < p.Price
                    ? p.SalePrice.Value : p.Price,
                inStock = p.StockQuantity > 0
            }).ToListAsync(cancellationToken);
        return Ok(new { items });
    }
}
