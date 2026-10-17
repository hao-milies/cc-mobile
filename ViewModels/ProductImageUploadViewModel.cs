using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace CCMobile.ViewModels
{
    public class ProductImageUploadViewModel
    {
        public int ProductId { get; set; }

        public string? ProductName { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ảnh.")]
        [Display(Name = "Ảnh sản phẩm")]
        public IFormFile? Image { get; set; }

        [StringLength(200,
            ErrorMessage = "Mô tả ảnh tối đa 200 ký tự.")]
        [Display(Name = "Mô tả ảnh")]
        public string? AltText { get; set; }
    }
}