
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyBanHangDienThoai.Models
{
    [Table("DanhMuc")]
    public class DanhMuc
    {
        [Key]
        public int MaDM { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên hãng.")]
        [StringLength(100, ErrorMessage = "Tên hãng tối đa 100 ký tự.")]
        public string TenDM { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự.")]
        public string? MoTa { get; set; }

        public ICollection<DienThoai> DienThoais { get; set; }
            = new List<DienThoai>();
    }
}
