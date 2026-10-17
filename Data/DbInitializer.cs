using CCMobile.Models;

namespace CCMobile.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ShopDbContext db)
        {
            const string sampleSku = "DEMO-PHONE-001";

            // Ẩn sản phẩm laptop mẫu cũ.
            var oldProduct = db.Products
                .FirstOrDefault(p => p.Sku == "DEMO-LAPTOP-001");

            if (oldProduct != null)
            {
                oldProduct.IsActive = false;
            }

            // Không thêm trùng điện thoại mẫu.
            if (db.Products.Any(p => p.Sku == sampleSku))
            {
                db.SaveChanges();
                return;
            }

            var category = db.Categories
                .FirstOrDefault(c => c.Name == "Điện thoại");

            if (category == null)
            {
                category = new Category
                {
                    Name = "Điện thoại",
                    Description = "Điện thoại thông minh."
                };
            }

            var brand = db.Brands
                .FirstOrDefault(b => b.Name == "Demo");

            if (brand == null)
            {
                brand = new Brand
                {
                    Name = "Demo",
                    Description = "Thương hiệu giả lập để kiểm thử."
                };
            }

            var product = new Product
            {
                Sku = sampleSku,
                Name = "Điện thoại mẫu 256 GB",
                Price = 12000000m,
                StockQuantity = 5,
                ShortDescription =
                    "RAM 8 GB • Bộ nhớ 256 GB • Màn hình 6,5 inch",
                Description =
                    "Sản phẩm giả lập để kiểm tra chức năng website.",
                Color = "Đen",
                WarrantyMonths = 12,
                IsActive = true,

                Category = category,
                Brand = brand,

                PhoneSpecification = new PhoneSpecification
                {
                    Chipset = "Chip mẫu",
                    RamGb = 8,
                    StorageGb = 256,
                    ScreenSizeInches = 6.5,
                    ScreenResolution = "1080 × 2400",
                    PanelType = "OLED",
                    RefreshRateHz = 120,
                    RearCamera = "Camera chính 50 MP",
                    FrontCamera = "12 MP",
                    BatteryMah = 5000,
                    ChargingWatts = 30,
                    OperatingSystem = "Android",
                    WeightGrams = 190
                }
            };

            db.Products.Add(product);
            db.SaveChanges();
        }
    }
}