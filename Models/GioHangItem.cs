namespace QuanLyBanHangDienThoai.Models
{
    public class GioHangItem
    {
        public int MaDT { get; set; }

        public string TenDT { get; set; } = "";

        public string? HinhAnh { get; set; }

        public decimal DonGia { get; set; }

        public int SoLuong { get; set; }

        public decimal ThanhTien => DonGia * SoLuong;
    }
}