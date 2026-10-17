using CCMobile.Models;
using Microsoft.AspNetCore.Identity;

namespace CCMobile.Data
{
    public static class AdminAccountInitializer
    {
        public static async Task InitializeAsync(
            IServiceProvider services,
            IConfiguration configuration)
        {
            var email = configuration["BootstrapAdmin:Email"]?.Trim();
            var password = configuration["BootstrapAdmin:Password"];

            // Máy không cấu hình tài khoản mẫu vẫn chạy được.
            if (string.IsNullOrWhiteSpace(email) &&
                string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "Cần cấu hình đủ email và mật khẩu Admin trong User Secrets.");
            }

            var userManager = services
                .GetRequiredService<UserManager<ApplicationUser>>();

            var existingUser = await userManager.FindByEmailAsync(email);

            if (existingUser != null)
            {
                if (!await userManager.IsInRoleAsync(existingUser, "Admin"))
                {
                    throw new InvalidOperationException(
                        "Email Admin đã thuộc một tài khoản không có quyền Admin. "
                        + "Hãy chọn email riêng khác trong User Secrets.");
                }

                return;
            }

            var db = services.GetRequiredService<ShopDbContext>();

            await using var transaction =
                await db.Database.BeginTransactionAsync();

            var admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = "Quản trị CC Mobile"
            };

            var createResult = await userManager.CreateAsync(
                admin, password);

            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Không tạo được Admin: " +
                    string.Join("; ",
                        createResult.Errors.Select(e => e.Description)));
            }

            var roleResult = await userManager.AddToRoleAsync(
                admin, "Admin");

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Không gán được quyền Admin: " +
                    string.Join("; ",
                        roleResult.Errors.Select(e => e.Description)));
            }

            await transaction.CommitAsync();
        }
    }
}