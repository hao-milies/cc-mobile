using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CCMobile.ViewModels
{
    public class ProductFormViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mã sản phẩm.")]
        [StringLength(50)]
        [Display(Name = "Mã sản phẩm")]
        public string Sku { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm.")]
        [StringLength(200)]
        [Display(Name = "Tên sản phẩm")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập giá bán.")]
        [Range(typeof(decimal), "1", "9999999999999999",
            ErrorMessage = "Giá bán phải lớn hơn 0 và nằm trong giới hạn cho phép.")]
        [Display(Name = "Giá bán (VNĐ)")]
        public decimal? Price { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng tồn.")]
        [Range(0, int.MaxValue,
            ErrorMessage = "Số lượng tồn không được âm.")]
        [Display(Name = "Số lượng tồn")]
        public int? StockQuantity { get; set; } = 0;

        [StringLength(500)]
        [Display(Name = "Mô tả ngắn")]
        public string? ShortDescription { get; set; }

        [Display(Name = "Mô tả chi tiết")]
        public string? Description { get; set; }

        [StringLength(50)]
        [Display(Name = "Màu sắc")]
        public string? Color { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập thời gian bảo hành.")]
        [Range(0, 120,
            ErrorMessage = "Thời gian bảo hành từ 0 đến 120 tháng.")]
        [Display(Name = "Bảo hành (tháng)")]
        public int? WarrantyMonths { get; set; } = 12;

        [Display(Name = "Hiển thị sản phẩm trên cửa hàng")]
        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
        [Range(1, int.MaxValue,
            ErrorMessage = "Vui lòng chọn danh mục hợp lệ.")]
        [Display(Name = "Danh mục")]
        public int? CategoryId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thương hiệu.")]
        [Range(1, int.MaxValue,
            ErrorMessage = "Vui lòng chọn thương hiệu hợp lệ.")]
        [Display(Name = "Thương hiệu")]
        public int? BrandId { get; set; }

        // Controller sẽ lấy các lựa chọn này từ database.
        public List<SelectListItem> CategoryOptions { get; set; } = new();

        public List<SelectListItem> BrandOptions { get; set; } = new();
    }
}