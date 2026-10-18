using System.ComponentModel.DataAnnotations;

namespace CCMobile.ViewModels
{
    public class LookupFormViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên.")]
        [StringLength(100, ErrorMessage = "Tên tối đa 100 ký tự.")]
        [Display(Name = "Tên")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự.")]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }
    }

    public class LookupListItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int ProductCount { get; set; }
    }
}
