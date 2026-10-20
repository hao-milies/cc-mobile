using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
namespace CCMobile.Controllers;
public class LanguageController : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Set(string culture, string? returnUrl)
    {
        if (culture is not ("vi" or "en")) return BadRequest();
        Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(
                new RequestCulture("vi-VN", culture == "en" ? "en-US" : "vi-VN")),
            new CookieOptions {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                HttpOnly = true, IsEssential = true, SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                Path = Request.PathBase.HasValue ? Request.PathBase.Value : "/"
            });
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Action("Index", "Products")!);
    }
}
