using CCMobile.Localization;
namespace CCMobile.Services;

public static class OrderWorkflow
{
    public static readonly IReadOnlyList<string> Statuses =
        Array.AsReadOnly(new[] { "Pending", "Confirmed", "Shipping", "Completed", "CancellationRequested", "Cancelled" });

    public static string Label(string status) => status switch
    {
        "Pending" => StoreText.T("Chờ xác nhận"),
        "Confirmed" => StoreText.T("Đã xác nhận"),
        "Shipping" => StoreText.T("Đang giao hàng"),
        "Completed" => StoreText.T("Hoàn tất"),
        "CancellationRequested" => StoreText.T("Chờ duyệt hủy"),
        "Cancelled" => StoreText.T("Đã hủy"),
        _ => StoreText.T("Không xác định")
    };

    public static bool CanMove(string current, string target) => (current, target) switch
    {
        ("Pending", "Confirmed") or
        ("Confirmed", "Shipping") or
        ("Shipping", "Completed") => true,
        _ => false
    };

    public static bool CanRequestCancellation(string status) => status is "Pending" or "Confirmed";

    public static string PaymentLabel(string status) => status switch
    {
        "Completed" => StoreText.T("Đã thu tiền COD"),
        "Cancelled" => StoreText.T("Đã hủy · Không thu tiền"),
        _ => StoreText.T("Chưa thu tiền COD")
    };
}
