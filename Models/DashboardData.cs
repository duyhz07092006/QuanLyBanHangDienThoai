namespace QuanLyBanHangDienThoai.Models;

public class DashboardData
{
    public int SoDienThoai { get; set; }
    public int SoKhachHang { get; set; }
    public int SoDonHangMoi { get; set; }
    public decimal DoanhThu { get; set; }
    public List<DonHang> DonHangMoi { get; set; } = new();
}
