using CCMobile.Data;
using CCMobile.Models;
using CCMobile.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CCMobile.Controllers
{

    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ShopDbContext _db;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ShopDbContext db)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _db = db;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var fullName = model.FullName.Trim();
            var email = model.Email.Trim();

            if (string.IsNullOrWhiteSpace(fullName))
            {
                ModelState.AddModelError(
                    nameof(model.FullName),
                    "Vui lòng nhập họ tên.");

                return View(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Email này đã được đăng ký.");

                return View(model);
            }

            var user = new ApplicationUser
            {
                FullName = fullName,
                UserName = email,
                Email = email
            };

            // Tạo tài khoản và gán vai trò phải cùng thành công.
            await using var transaction =
                await _db.Database.BeginTransactionAsync();

            var createResult = await _userManager.CreateAsync(
                user, model.Password);

            if (!createResult.Succeeded)
            {
                await transaction.RollbackAsync();

                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty, error.Description);
                }

                return View(model);
            }

            // Vai trò được cố định ở server, không lấy từ form.
            var roleResult = await _userManager.AddToRoleAsync(
                user, "User");

            if (!roleResult.Succeeded)
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "Chưa thể hoàn tất đăng ký. Vui lòng thử lại sau.");

                return View(model);
            }

            await transaction.CommitAsync();

            TempData["SuccessMessage"] =
                "Đăng ký thành công. Bạn có thể đăng nhập ngay.";

            return RedirectToAction(nameof(Login));
        }
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            return View(new LoginViewModel
            {
                ReturnUrl = returnUrl
            });
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(
                model.Email.Trim());

            if (user == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Email hoặc mật khẩu không chính xác.");

                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user,
                model.Password,
                isPersistent: model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                // Chỉ chuyển hướng đến địa chỉ nội bộ của website.
                if (!string.IsNullOrEmpty(model.ReturnUrl)
                    && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return LocalRedirect(model.ReturnUrl);
                }

                return RedirectToAction("Index", "Products");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Tài khoản tạm khóa do đăng nhập sai nhiều lần. "
                    + "Vui lòng thử lại sau 5 phút.");
            }
            else if (result.IsNotAllowed)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Tài khoản hiện chưa được phép đăng nhập.");
            }
            else
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Email hoặc mật khẩu không chính xác.");
            }

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction("Index", "Products");
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccessDenied()
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return View();
        }
    }
}