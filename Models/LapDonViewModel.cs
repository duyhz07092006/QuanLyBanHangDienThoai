
using System.ComponentModel.DataAnnotations;

namespace QuanLyBanHangDienThoai.Models
{
    public class LapDonViewModel
    {
        [Required(ErrorMessage = "Nhập số điện thoại khách hàng")]
        public string SoDienThoai { get; set; } = "";

        [Required(ErrorMessage = "Nhập họ tên khách hàng")]
        public string HoTen { get; set; } = "";

        public string DiaChi { get; set; } = "";

        [Range(1, int.MaxValue, ErrorMessage = "Chọn nhân viên lập đơn")]
        public int MaNV { get; set; }

        public string PhuongThucTT { get; set; } = "Tiền mặt";

        public List<ChiTietLapDonViewModel> SanPham { get; set; } = new();
    }

    public class ChiTietLapDonViewModel
    {
        public int MaDT { get; set; }
        public string TenDT { get; set; } = "";
        public decimal DonGia { get; set; }
        public int SoLuongTon { get; set; }

        [Range(0, 1000, ErrorMessage = "Số lượng không hợp lệ")]
        public int SoLuong { get; set; }
    }
}
