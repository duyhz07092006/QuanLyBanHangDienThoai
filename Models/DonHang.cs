using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyBanHangDienThoai.Models
{
    [Table("DonHang")]
    public class DonHang
    {
        [Key]
        public int MaDH { get; set; }

        public int? MaKH { get; set; }

        public int? MaNV { get; set; }

        public DateTime NgayDat { get; set; }

        public string TenNguoiNhan { get; set; } = "";

        public string SdtNguoiNhan { get; set; } = "";

        [Column("DiaChiGiao")]
        public string DiaChiGiaoHang { get; set; } = "";

        public string PhuongThucTT { get; set; } = "COD";

        public decimal TongTien { get; set; }

        public string TrangThai { get; set; } = "";

        public string? GhiChu { get; set; }

        public List<ChiTietDonHang> ChiTiet { get; set; } = new();
    }

    [Table("ChiTietDonHang")]
    public class ChiTietDonHang
    {
        public int MaDH { get; set; }

        public int MaDT { get; set; }

        public int SoLuong { get; set; }

        public decimal DonGia { get; set; }

        public DonHang? DonHang { get; set; }
        [NotMapped]
        public string TenSanPham { get; set; } = "";

        public DienThoai? DienThoai { get; set; }
    }
}