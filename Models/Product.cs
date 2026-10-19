namespace CCMobile.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã sản phẩm.")]
        [StringLength(50)]
        public string Sku { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm.")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        [Range(typeof(decimal), "1", "9999999999999999",
            ErrorMessage = "Giá phải lớn hơn 0 và nằm trong giới hạn cho phép.")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? SalePrice { get; set; }

        [NotMapped]
        public bool HasDiscount => SalePrice.HasValue && SalePrice.Value > 0 && SalePrice.Value < Price;
        [NotMapped]
        public decimal EffectivePrice => HasDiscount ? SalePrice!.Value : Price;
        [NotMapped]
        public decimal DiscountPercent => HasDiscount
            ? decimal.Floor((Price - EffectivePrice) / Price * 1000m) / 10m : 0;


        [Range(0, int.MaxValue,
            ErrorMessage = "Số lượng tồn kho không được âm.")]
        public int StockQuantity { get; set; }

        [StringLength(500)]
        public string? ShortDescription { get; set; }

        public string? Description { get; set; }

        [StringLength(50)]
        public string? Color { get; set; }

        [Range(0, 120,
            ErrorMessage = "Thời gian bảo hành từ 0 đến 120 tháng.")]
        public int WarrantyMonths { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Liên kết đến categori
        [Range(1, int.MaxValue,
            ErrorMessage = "Vui lòng chọn danh mục.")]
        public int CategoryId { get; set; }

        public Category Category { get; set; } = null!;

        // Liên kết đến brand
        [Range(1, int.MaxValue,
            ErrorMessage = "Vui lòng chọn thương hiệu.")]
        public int BrandId { get; set; }

        public Brand Brand { get; set; } = null!;
        public ICollection<ProductImage> Images { get; set; }
    = new List<ProductImage>();
        public PhoneSpecification? PhoneSpecification { get; set; }
    }
}
