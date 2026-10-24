namespace QuanLyBanHangDienThoai.Services;

public static class AnhSanPham
{
    // Cột HinhAnh trong database có thể là tên file trần (iphone15.jpg),
    // đường dẫn tuyệt đối (/images/a.jpg) hoặc link http(s).
    // Trả về đường dẫn dùng được trong view (qua Url.Content), hoặc null nếu chưa có ảnh.
    public static string? DuongDan(string? hinhAnh)
    {
        var ten = hinhAnh?.Trim();
        if (string.IsNullOrWhiteSpace(ten)) return null;

        if (ten.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            ten.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            ten.StartsWith("/") || ten.StartsWith("~/"))
        {
            return ten;
        }

        return "~/images/" + ten;
    }
}
