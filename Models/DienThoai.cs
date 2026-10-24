using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyBanHangDienThoai.Models
{
    [Table("DienThoai")]
    public class DienThoai
    {
        [Key]
        public int MaDT { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên điện thoại.")]
        [StringLength(150, MinimumLength = 2,
            ErrorMessage = "Tên điện thoại phải từ 2 đến 150 ký tự.")]
        [Display(Name = "Tên điện thoại")]
        public string TenDT { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn hãng điện thoại.")]
        [Display(Name = "Hãng")]
        public int MaDM { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá bán.")]
        [Range(typeof(decimal), "1000", "1000000000",
            ErrorMessage = "Giá bán phải từ 1.000 đến 1.000.000.000 đồng.")]
        [Column(TypeName = "decimal(18,0)")]
        [Display(Name = "Giá bán")]
        public decimal Gia { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng tồn.")]
        [Range(0, 100000, ErrorMessage = "Số lượng tồn phải từ 0 đến 100.000.")]
        [Display(Name = "Số lượng tồn")]
        public int SoLuongTon { get; set; }

        [StringLength(255, ErrorMessage = "Đường dẫn hình ảnh tối đa 255 ký tự.")]
        public string? HinhAnh { get; set; }

        [StringLength(100, ErrorMessage = "Màn hình tối đa 100 ký tự.")]
        public string? ManHinh { get; set; }

        [StringLength(50, ErrorMessage = "RAM tối đa 50 ký tự.")]
        public string? RAM { get; set; }

        [StringLength(50, ErrorMessage = "Bộ nhớ tối đa 50 ký tự.")]
        public string? BoNho { get; set; }

        [StringLength(100, ErrorMessage = "Pin tối đa 100 ký tự.")]
        public string? Pin { get; set; }

        [StringLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự.")]
        public string? MoTa { get; set; }

        public DateTime NgayTao { get; set; }

        public bool TrangThai { get; set; }

        [ForeignKey(nameof(MaDM))]
        public DanhMuc? DanhMuc { get; set; }
    }
}
