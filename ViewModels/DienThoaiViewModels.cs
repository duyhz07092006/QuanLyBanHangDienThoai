using System.ComponentModel.DataAnnotations;
using QuanLyBanHangDienThoai.Models;

namespace QuanLyBanHangDienThoai.ViewModels
{
    // Tham số tìm kiếm / lọc / sắp xếp / phân trang của trang Danh sách điện thoại
    public class DienThoaiLocViewModel
    {
        public string? TuKhoa { get; set; }
        public int? MaDM { get; set; }
        public decimal? GiaTu { get; set; }
        public decimal? GiaDen { get; set; }
        public string? SapXep { get; set; }
        public int Trang { get; set; } = 1;
    }

    // Model của view DienThoai/Index
    public class DienThoaiIndexViewModel
    {
        public DienThoaiLocViewModel Loc { get; set; } = new();
        public KetQuaPhanTrang<DienThoai> KetQua { get; set; } = new();
        public List<DanhMuc> DanhMuc { get; set; } = new();
        public string? CanhBao { get; set; }
    }

    // ----- DTO cho RESTful API -----

    public class DienThoaiDto
    {
        public int MaDT { get; set; }
        public string TenDT { get; set; } = "";
        public int MaDM { get; set; }
        public string? TenHang { get; set; }
        public decimal Gia { get; set; }
        public int SoLuongTon { get; set; }
        public string? HinhAnh { get; set; }
        public string? ManHinh { get; set; }
        public string? RAM { get; set; }
        public string? BoNho { get; set; }
        public string? Pin { get; set; }
        public string? MoTa { get; set; }
        public DateTime NgayTao { get; set; }
        public bool TrangThai { get; set; }
    }

    // Dữ liệu client gửi lên khi thêm / sửa (POST, PUT)
    public class DienThoaiInputDto
    {
        [Required(ErrorMessage = "Vui lòng nhập tên điện thoại.")]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "Tên điện thoại phải từ 2 đến 150 ký tự.")]
        public string TenDT { get; set; } = "";

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn hãng điện thoại.")]
        public int MaDM { get; set; }

        [Range(typeof(decimal), "1000", "1000000000",
            ErrorMessage = "Giá bán phải từ 1.000 đến 1.000.000.000 đồng.")]
        public decimal Gia { get; set; }

        [Range(0, 100000, ErrorMessage = "Số lượng tồn phải từ 0 đến 100.000.")]
        public int SoLuongTon { get; set; }

        [StringLength(255)] public string? HinhAnh { get; set; }
        [StringLength(100)] public string? ManHinh { get; set; }
        [StringLength(50)]  public string? RAM { get; set; }
        [StringLength(50)]  public string? BoNho { get; set; }
        [StringLength(100)] public string? Pin { get; set; }
        [StringLength(1000)] public string? MoTa { get; set; }

        public bool TrangThai { get; set; } = true;
    }
}
