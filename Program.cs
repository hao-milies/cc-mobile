using System.Globalization;
using Microsoft.AspNetCore.Localization;
using CCMobile.Localization;
using Microsoft.EntityFrameworkCore;
using CCMobile.Data;
using Microsoft.AspNetCore.Identity;
using CCMobile.Models;
using CCMobile.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("ShopConnection")
    ?? throw new InvalidOperationException(
        "Chưa cấu hình chuỗi kết nối ShopConnection.");

builder.Services.AddDbContext<ShopDbContext>(options =>
    options.UseSqlServer(connectionString));
// Add services to the container.
builder.Services.AddLocalization();
builder.Services.AddControllersWithViews()
    .AddDataAnnotationsLocalization(options =>
        options.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(StoreText)));
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("vi-VN", "vi-VN");
    // Keep VND, decimal input and date conventions stable; translate interface text only.
    options.SupportedCultures = new[] { CultureInfo.GetCultureInfo("vi-VN") };
    options.SupportedUICultures = new[] {
        CultureInfo.GetCultureInfo("vi-VN"), CultureInfo.GetCultureInfo("en-US") };
    options.RequestCultureProviders = new List<IRequestCultureProvider> {
        new CookieRequestCultureProvider() };
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.Name = ".CCMobile.CartSession";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(5);
    })
    .AddEntityFrameworkStores<ShopDbContext>()
.AddErrorDescriber<VietnameseIdentityErrorDescriber>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";

    options.ExpireTimeSpan = TimeSpan.FromHours(2);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
});

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    // Vai trò cần tồn tại ở mọi môi trường.
    await IdentityInitializer.InitializeAsync(
        scope.ServiceProvider);

    // Dữ liệu sản phẩm mẫu chỉ tạo khi phát triển.
    if (app.Environment.IsDevelopment())
    {
        var db = scope.ServiceProvider
            .GetRequiredService<ShopDbContext>();

        DbInitializer.Initialize(db);
        await AdminAccountInitializer.InitializeAsync(
            scope.ServiceProvider,
            app.Configuration);
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRequestLocalization();
app.UseRouting();
app.UseSession();

app.UseAuthentication();


app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
