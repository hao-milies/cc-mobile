namespace CCMobile.Data
{
    using Microsoft.EntityFrameworkCore;
    using CCMobile.Models;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    public class ShopDbContext : IdentityDbContext<ApplicationUser>
    {
        public ShopDbContext(DbContextOptions<ShopDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Brand> Brands => Set<Brand>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductImage> ProductImages => Set<ProductImage>();
        public DbSet<PhoneSpecification> PhoneSpecifications
            => Set<PhoneSpecification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Mã sản phẩm không được trùng.
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Sku)
                .IsUnique();

            // Một danh mục có nhiều sản phẩm.
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Một thương hiệu có nhiều sản phẩm.
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Brand)
                .WithMany()
                .HasForeignKey(p => p.BrandId)
                .OnDelete(DeleteBehavior.Restrict);

            // Một sản phẩm có nhiều ảnh.
            modelBuilder.Entity<ProductImage>()
                .HasOne(i => i.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Mỗi sản phẩm có tối đa một bản thông số điện thoại.
            modelBuilder.Entity<PhoneSpecification>()
                .HasOne(s => s.Product)
                .WithOne(p => p.PhoneSpecification)
                .HasForeignKey<PhoneSpecification>(s => s.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Mỗi sản phẩm có tối đa một ảnh đại diện.
            modelBuilder.Entity<ProductImage>()
                .HasIndex(i => i.ProductId)
                .IsUnique()
                .HasFilter("[IsPrimary] = 1");
        }
    }
}
