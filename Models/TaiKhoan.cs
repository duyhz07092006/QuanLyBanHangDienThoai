using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyBanHangDienThoai.Models
{
    [Table("TaiKhoan")]
    public class TaiKhoan
    {
        [Key]
        public int MaTK { get; set; }

        public string TenDangNhap { get; set; } = string.Empty;

        public string MatKhau { get; set; } = string.Empty;

        public string VaiTro { get; set; } = string.Empty;

        public bool TrangThai { get; set; }

        public int? MaNV { get; set; }

        public int? MaKH { get; set; }
    }
}