namespace QuanLyBanHangDienThoai.Models;

public class StoreDienThoai
{
    public int MaDT { get; set; }
    public string TenDT { get; set; } = "";
    public string TenDM { get; set; } = "";
    public decimal Gia { get; set; }
    public int SoLuongTon { get; set; }
    public string? HinhAnh { get; set; }
    public string? ManHinh { get; set; }
    public string? RAM { get; set; }
    public string? BoNho { get; set; }
    public string? Pin { get; set; }
    public string? MoTa { get; set; }
}
