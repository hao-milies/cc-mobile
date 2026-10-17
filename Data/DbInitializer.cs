using CCMobile.Models;

namespace CCMobile.Data
{
    public static class DbInitializer
    {
        private static void EnsureSampleImages(ShopDbContext db, int productId)
        {
            var existingImages = db.ProductImages
                .Where(i => i.ProductId == productId)
                .ToList();

            var samples = new[]
            {
        new
        {
            Url = "/images/products/phone-demo-front.jpg",
            Alt = "Điện thoại mẫu nhìn từ phía trước",
            Order = 0
        },
        new
        {
            Url = "/images/products/phone-demo-back.jpg",
            Alt = "Điện thoại mẫu nhìn từ phía sau",
            Order = 1
        }
    };

            var hasPrimary = existingImages.Any(i => i.IsPrimary);

            foreach (var sample in samples)
            {
                var image = existingImages
                    .FirstOrDefault(i => i.ImageUrl == sample.Url);

                if (image == null)
                {
                    image = new ProductImage
                    {
                        ProductId = productId,
                        ImageUrl = sample.Url,
                        AltText = sample.Alt,
                        DisplayOrder = sample.Order,
                        IsPrimary = false
                    };

                    db.ProductImages.Add(image);
                    existingImages.Add(image);
                }

                if (!hasPrimary)
                {
                    image.IsPrimary = true;
                    hasPrimary = true;
                }
            }

            db.SaveChanges();
        }
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
            var existingProduct = db.Products
    .FirstOrDefault(p => p.Sku == sampleSku);

            if (existingProduct != null)
            {
                db.SaveChanges();
                EnsureSampleImages(db, existingProduct.Id);
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
            EnsureSampleImages(db, product.Id);
        }
    }
}