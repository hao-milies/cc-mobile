namespace CCMobile.Services;

public static class OrderWorkflow
{
    public static readonly IReadOnlyList<string> Statuses =
        Array.AsReadOnly(new[] { "Pending", "Confirmed", "Shipping", "Completed", "CancellationRequested", "Cancelled" });

    public static string Label(string status) => status switch
    {
        "Pending" => "Chờ xác nhận",
        "Confirmed" => "Đã xác nhận",
        "Shipping" => "Đang giao hàng",
        "Completed" => "Hoàn tất",
        "CancellationRequested" => "Chờ duyệt hủy",
        "Cancelled" => "Đã hủy",
        _ => "Không xác định"
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
        "Completed" => "Đã thu tiền COD",
        "Cancelled" => "Đã hủy · Không thu tiền",
        _ => "Chưa thu tiền COD"
    };
}
