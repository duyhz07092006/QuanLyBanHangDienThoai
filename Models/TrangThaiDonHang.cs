namespace QuanLyBanHangDienThoai.Models;

// Các trạng thái đơn hàng (khớp ràng buộc CHECK của bảng DonHang) và luồng chuyển hợp lệ:
//   Chờ xử lý -> Đang giao -> Hoàn thành
//   Chờ xử lý / Đang giao -> Đã hủy
public static class TrangThaiDonHang
{
    public const string ChoXuLy = "Chờ xử lý";
    public const string DangGiao = "Đang giao";
    public const string HoanThanh = "Hoàn thành";
    public const string DaHuy = "Đã hủy";

    public static IReadOnlyList<string> BuocTiepTheo(string? hienTai)
    {
        return hienTai switch
        {
            ChoXuLy => new[] { DangGiao, DaHuy },
            DangGiao => new[] { HoanThanh, DaHuy },
            _ => Array.Empty<string>()
        };
    }

    public static bool CoTheChuyen(string? hienTai, string? moi)
        => moi is not null && BuocTiepTheo(hienTai).Contains(moi);
}

public enum UpdateOrderStatusResult
{
    Updated,
    NotFound,
    InvalidTransition
}
