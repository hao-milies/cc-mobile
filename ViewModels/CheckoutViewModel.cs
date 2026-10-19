using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CCMobile.ViewModels;

public class CheckoutViewModel
{
    [Required(ErrorMessage = "Phiên đặt hàng không hợp lệ. Vui lòng mở lại trang đặt hàng.")]
    public string Ticket { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận.")]
    [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
    [Display(Name = "Họ tên người nhận")]
    public string RecipientName { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [RegularExpression(@"\+?[0-9]{9,15}", ErrorMessage = "Nhập 9–15 chữ số, có thể bắt đầu bằng dấu +.")]
    [Display(Name = "Số điện thoại")]
    public string PhoneNumber { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận hàng.")]
    [StringLength(500, ErrorMessage = "Địa chỉ tối đa 500 ký tự.")]
    [Display(Name = "Địa chỉ nhận hàng")]
    public string ShippingAddress { get; set; } = "";
    [StringLength(1000, ErrorMessage = "Ghi chú tối đa 1000 ký tự.")]
    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }
    [ValidateNever] public List<CheckoutLine> Lines { get; set; } = new();
    public decimal Total => Lines.Sum(x => x.Price * x.Quantity);
}

public class CheckoutLine
{
    public int ProductId { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public class CheckoutTicket
{
    public Guid Key { get; set; }
    public string UserId { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public List<CheckoutLine> Lines { get; set; } = new();
}
