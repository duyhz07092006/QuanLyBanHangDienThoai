
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyBanHangDienThoai.Models
{
    [Table("NhanVien")]
    public class NhanVien
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaNV { get; set; }

        [Required(ErrorMessage = "Nhập họ tên nhân viên")]
        [StringLength(100)]
        public string HoTen { get; set; } = "";

        [StringLength(10)]
        public string? GioiTinh { get; set; }

        public DateTime? NgaySinh { get; set; }

        [Required(ErrorMessage = "Nhập số điện thoại")]
        [StringLength(11)]
        public string SoDienThoai { get; set; } = "";

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(255)]
        public string? DiaChi { get; set; }

        [StringLength(100)]
        public string? ChucVu { get; set; }
    }
}
