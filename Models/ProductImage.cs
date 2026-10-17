namespace CCMobile.Models

{
    using System.ComponentModel.DataAnnotations;
    public class ProductImage
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ảnh sản phẩm.")]
        [StringLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        [StringLength(200)]
        public string? AltText { get; set; }

        [Range(0, int.MaxValue)]
        public int DisplayOrder { get; set; }

        public bool IsPrimary { get; set; } = false;

        // Sản phẩm sở hữu ảnh
        [Range(1, int.MaxValue)]
        public int ProductId { get; set; }

        public Product Product { get; set; } = null!;
    }
}
