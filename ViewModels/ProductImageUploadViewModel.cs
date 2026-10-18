using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace CCMobile.ViewModels
{
    public class ProductImageUploadViewModel
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }

        [Display(Name = "Ảnh sản phẩm")]
        public List<IFormFile> Images { get; set; } = new();
    }
}
