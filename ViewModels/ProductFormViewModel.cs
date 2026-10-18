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


        [Display(Name = "Có thông số điện thoại")]
        public bool HasPhoneSpecification { get; set; }


        [Display(Name = "Chip xử lý")]
        [StringLength(200, ErrorMessage = "Chip xử lý tối đa 200 ký tự.")]
        public string? PhoneChipset { get; set; }

        [Display(Name = "RAM (GB)")]
        [Range(1, 128, ErrorMessage = "RAM (GB) phải từ 1 đến 128.")]
        public int? PhoneRamGb { get; set; }

        [Display(Name = "Bộ nhớ (GB)")]
        [Range(1, 8192, ErrorMessage = "Bộ nhớ (GB) phải từ 1 đến 8192.")]
        public int? PhoneStorageGb { get; set; }

        [Display(Name = "Màn hình (inch)")]
        [Range(1, 15, ErrorMessage = "Màn hình (inch) phải từ 1 đến 15.")]
        public double? PhoneScreenSizeInches { get; set; }

        [Display(Name = "Độ phân giải")]
        [StringLength(100, ErrorMessage = "Độ phân giải tối đa 100 ký tự.")]
        public string? PhoneScreenResolution { get; set; }

        [Display(Name = "Tấm nền")]
        [StringLength(100, ErrorMessage = "Tấm nền tối đa 100 ký tự.")]
        public string? PhonePanelType { get; set; }

        [Display(Name = "Tần số quét (Hz)")]
        [Range(1, 1000, ErrorMessage = "Tần số quét (Hz) phải từ 1 đến 1000.")]
        public int? PhoneRefreshRateHz { get; set; }

        [Display(Name = "Camera sau")]
        [StringLength(300, ErrorMessage = "Camera sau tối đa 300 ký tự.")]
        public string? PhoneRearCamera { get; set; }

        [Display(Name = "Camera trước")]
        [StringLength(200, ErrorMessage = "Camera trước tối đa 200 ký tự.")]
        public string? PhoneFrontCamera { get; set; }

        [Display(Name = "Dung lượng pin (mAh)")]
        [Range(1, 50000, ErrorMessage = "Dung lượng pin (mAh) phải từ 1 đến 50000.")]
        public int? PhoneBatteryMah { get; set; }

        [Display(Name = "Công suất sạc (W)")]
        [Range(1, 500, ErrorMessage = "Công suất sạc (W) phải từ 1 đến 500.")]
        public int? PhoneChargingWatts { get; set; }

        [Display(Name = "Hệ điều hành")]
        [StringLength(100, ErrorMessage = "Hệ điều hành tối đa 100 ký tự.")]
        public string? PhoneOperatingSystem { get; set; }

        [Display(Name = "Khối lượng (g)")]
        [Range(1, 2000, ErrorMessage = "Khối lượng (g) phải từ 1 đến 2000.")]
        public double? PhoneWeightGrams { get; set; }


        // Controller sẽ lấy các lựa chọn này từ database.
        public List<SelectListItem> CategoryOptions { get; set; } = new();

        public List<SelectListItem> BrandOptions { get; set; } = new();
    }
}