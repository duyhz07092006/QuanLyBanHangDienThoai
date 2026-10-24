
using System.ComponentModel.DataAnnotations;

namespace QuanLyBanHangDienThoai.ViewModels
{
    public class DatHangViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên người nhận.")]
        [StringLength(100)]
        [Display(Name = "Tên người nhận")]
        public string TenNguoiNhan { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [StringLength(20)]
        [Display(Name = "Số điện thoại")]
        public string SdtNguoiNhan { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng.")]
        [StringLength(300)]
        [Display(Name = "Địa chỉ giao hàng")]
        public string DiaChiGiao { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
        [Display(Name = "Phương thức thanh toán")]
        public string PhuongThucTT { get; set; } = "COD";

        [StringLength(500)]
        [Display(Name = "Ghi chú")]
        public string? GhiChu { get; set; }
    }
}
