using Microsoft.EntityFrameworkCore;

namespace QuanLyBanHangDienThoai.Models;

// Kết quả một trang dữ liệu (dùng chung cho giao diện MVC và RESTful API)
public class KetQuaPhanTrang<T>
{
    public List<T> DuLieu { get; set; } = new();
    public int TrangHienTai { get; set; } = 1;
    public int KichThuocTrang { get; set; } = 10;
    public int TongSoDong { get; set; }

    public int TongSoTrang =>
        KichThuocTrang <= 0 ? 0 : (int)Math.Ceiling(TongSoDong / (double)KichThuocTrang);

    // Số thứ tự dòng đầu/cuối của trang hiện tại (hiển thị "Hiển thị 1-8 / 15")
    public int TuDong => TongSoDong == 0 ? 0 : (TrangHienTai - 1) * KichThuocTrang + 1;
    public int DenDong => TuDong == 0 ? 0 : TuDong + DuLieu.Count - 1;

    public static async Task<KetQuaPhanTrang<T>> TaoAsync(
        IQueryable<T> nguon, int trang, int kichThuoc)
    {
        kichThuoc = Math.Max(1, kichThuoc);

        int tongDong = await nguon.CountAsync();
        int tongTrang = Math.Max(1, (int)Math.Ceiling(tongDong / (double)kichThuoc));
        trang = Math.Clamp(trang, 1, tongTrang);

        var duLieu = await nguon
            .Skip((trang - 1) * kichThuoc)
            .Take(kichThuoc)
            .ToListAsync();

        return new KetQuaPhanTrang<T>
        {
            DuLieu = duLieu,
            TrangHienTai = trang,
            KichThuocTrang = kichThuoc,
            TongSoDong = tongDong
        };
    }
}
