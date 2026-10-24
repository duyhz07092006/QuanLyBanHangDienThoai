using QuanLyBanHangDienThoai.Models;

namespace QuanLyBanHangDienThoai.Services;

// Điều kiện tìm kiếm / lọc / sắp xếp điện thoại, dùng chung cho MVC và API
public static class DienThoaiQueryExtensions
{
    public static IQueryable<DienThoai> Loc(
        this IQueryable<DienThoai> query,
        string? tuKhoa, int? maDM, decimal? giaTu, decimal? giaDen)
    {
        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var kw = tuKhoa.Trim();
            query = query.Where(d =>
                d.TenDT.Contains(kw) ||
                (d.DanhMuc != null && d.DanhMuc.TenDM.Contains(kw)));
        }

        if (maDM.HasValue && maDM.Value > 0)
            query = query.Where(d => d.MaDM == maDM.Value);

        if (giaTu.HasValue)
            query = query.Where(d => d.Gia >= giaTu.Value);

        if (giaDen.HasValue)
            query = query.Where(d => d.Gia <= giaDen.Value);

        return query;
    }

    public static IQueryable<DienThoai> SapXepTheo(
        this IQueryable<DienThoai> query, string? sapXep)
    {
        return sapXep switch
        {
            "gia_tang" => query.OrderBy(d => d.Gia).ThenBy(d => d.MaDT),
            "gia_giam" => query.OrderByDescending(d => d.Gia).ThenBy(d => d.MaDT),
            "ten"      => query.OrderBy(d => d.TenDT).ThenBy(d => d.MaDT),
            "moi"      => query.OrderByDescending(d => d.NgayTao).ThenByDescending(d => d.MaDT),
            _          => query.OrderBy(d => d.MaDT)
        };
    }
}
