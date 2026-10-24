
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using QuanLyBanHangDienThoai.Models;
using QuanLyBanHangDienThoai.Services;
using System.Data;

namespace QuanLyBanHangDienThoai.Controllers
{
    public class LapDonController : Controller
    {
        private readonly IConfiguration _config;
        private readonly StoreRepository _store;

        public LapDonController(
            IConfiguration config, StoreRepository store)
        {
            _config = config;
            _store = store;
        }

        private string ConnectionString =>
            _config.GetConnectionString("QLBanDienThoai")!;

        private async Task NapDuLieu(LapDonViewModel model)
        {
            var phones = await _store.GetPhonesAsync();

            model.SanPham = phones.Select(p => new ChiTietLapDonViewModel
            {
                MaDT = p.MaDT,
                TenDT = p.TenDT,
                DonGia = p.Gia,
                SoLuongTon = p.SoLuongTon,
                SoLuong = model.SanPham
                    .FirstOrDefault(x => x.MaDT == p.MaDT)?.SoLuong ?? 0
            }).ToList();

            var employees = new List<object>();

            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(
                "SELECT MaNV, HoTen FROM dbo.NhanVien ORDER BY HoTen", conn);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                employees.Add(new
                {
                    MaNV = reader.GetInt32(0),
                    HoTen = reader.GetString(1)
                });
            }

            ViewBag.NhanVien = employees;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new LapDonViewModel();
            await NapDuLieu(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LapDonViewModel model)
        {
            var selected = model.SanPham
                .Where(p => p.SoLuong > 0).ToList();

            if (selected.Count == 0)
                ModelState.AddModelError("", "Hãy chọn ít nhất một điện thoại.");

            if (string.IsNullOrWhiteSpace(model.SoDienThoai) ||
                model.SoDienThoai.Length > 11)
                ModelState.AddModelError(nameof(model.SoDienThoai),
                    "Số điện thoại không hợp lệ.");

            if (model.PhuongThucTT != "Tiền mặt" &&
                model.PhuongThucTT != "Chuyển khoản" &&
                model.PhuongThucTT != "COD")
                ModelState.AddModelError(nameof(model.PhuongThucTT),
                    "Phương thức thanh toán không hợp lệ.");

            if (!ModelState.IsValid)
            {
                await NapDuLieu(model);
                return View(model);
            }

            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();
            await using var tran =
                (SqlTransaction)await conn.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                // Kiểm tra nhân viên
                await using (var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM dbo.NhanVien WHERE MaNV=@MaNV",
                    conn, tran))
                {
                    cmd.Parameters.AddWithValue("@MaNV", model.MaNV);
                    if ((int)await cmd.ExecuteScalarAsync()! == 0)
                        throw new InvalidOperationException(
                            "Nhân viên không tồn tại.");
                }

                // Tìm khách hàng theo số điện thoại
                int maKH;

                await using (var cmd = new SqlCommand(
                    "SELECT MaKH FROM dbo.KhachHang WHERE SoDienThoai=@SDT",
                    conn, tran))
                {
                    cmd.Parameters.Add("@SDT", SqlDbType.VarChar, 11)
                        .Value = model.SoDienThoai.Trim();

                    var result = await cmd.ExecuteScalarAsync();

                    if (result != null)
                    {
                        maKH = Convert.ToInt32(result);

                        await using var update = new SqlCommand(
                            @"UPDATE dbo.KhachHang
                              SET HoTen=@Ten, DiaChi=@DiaChi
                              WHERE MaKH=@MaKH", conn, tran);

                        update.Parameters.AddWithValue("@Ten", model.HoTen.Trim());
                        update.Parameters.AddWithValue("@DiaChi",
                            model.DiaChi ?? "");
                        update.Parameters.AddWithValue("@MaKH", maKH);
                        await update.ExecuteNonQueryAsync();
                    }
                    else
                    {
                        await using var insert = new SqlCommand(
                            @"INSERT INTO dbo.KhachHang
                              (HoTen, SoDienThoai, DiaChi)
                              OUTPUT INSERTED.MaKH
                              VALUES (@Ten, @SDT, @DiaChi)",
                            conn, tran);

                        insert.Parameters.AddWithValue("@Ten", model.HoTen.Trim());
                        insert.Parameters.Add("@SDT", SqlDbType.VarChar, 11)
                            .Value = model.SoDienThoai.Trim();
                        insert.Parameters.AddWithValue("@DiaChi",
                            model.DiaChi ?? "");

                        maKH = Convert.ToInt32(
                            await insert.ExecuteScalarAsync());
                    }
                }

                decimal tongTien = 0;

                // Khóa dòng sản phẩm và kiểm tra tồn kho thật
                var lines = new List<(int MaDT, int SoLuong, decimal Gia)>();

                foreach (var item in selected)
                {
                    await using var cmd = new SqlCommand(
                        @"SELECT Gia, SoLuongTon
                          FROM dbo.DienThoai WITH (UPDLOCK, HOLDLOCK)
                          WHERE MaDT=@MaDT AND TrangThai=1",
                        conn, tran);

                    cmd.Parameters.AddWithValue("@MaDT", item.MaDT);

                    await using var reader = await cmd.ExecuteReaderAsync();

                    if (!await reader.ReadAsync())
                        throw new InvalidOperationException(
                            $"Điện thoại mã {item.MaDT} không tồn tại hoặc đã ngừng bán.");

                    decimal gia = reader.GetDecimal(0);
                    int ton = reader.GetInt32(1);

                    await reader.CloseAsync();

                    if (item.SoLuong > ton)
                        throw new InvalidOperationException(
                            $"Điện thoại mã {item.MaDT} không đủ tồn kho.");

                    lines.Add((item.MaDT, item.SoLuong, gia));
                    tongTien += gia * item.SoLuong;
                }

                // Tạo đơn hàng tại quầy
                int maDH;

                await using (var cmd = new SqlCommand(
                    @"INSERT INTO dbo.DonHang
                      (MaKH, MaNV, TenNguoiNhan, SdtNguoiNhan,
                       DiaChiGiao, PhuongThucTT, TongTien, TrangThai, GhiChu)
                      OUTPUT INSERTED.MaDH
                      VALUES
                      (@MaKH, @MaNV, @Ten, @SDT,
                       @DiaChi, @PTTT, @TongTien, N'Hoàn thành', N'Mua tại quầy')",
                    conn, tran))
                {
                    cmd.Parameters.AddWithValue("@MaKH", maKH);
                    cmd.Parameters.AddWithValue("@MaNV", model.MaNV);
                    cmd.Parameters.AddWithValue("@Ten", model.HoTen.Trim());
                    cmd.Parameters.Add("@SDT", SqlDbType.VarChar, 11)
                        .Value = model.SoDienThoai.Trim();
                    cmd.Parameters.AddWithValue("@DiaChi",
                        string.IsNullOrWhiteSpace(model.DiaChi)
                            ? "Mua tại cửa hàng" : model.DiaChi.Trim());
                    cmd.Parameters.AddWithValue("@PTTT", model.PhuongThucTT);
                    cmd.Parameters.AddWithValue("@TongTien", tongTien);

                    maDH = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                foreach (var line in lines)
                {
                    await using var detail = new SqlCommand(
                        @"INSERT INTO dbo.ChiTietDonHang
                          (MaDH, MaDT, SoLuong, DonGia)
                          VALUES (@MaDH, @MaDT, @SL, @Gia)",
                        conn, tran);

                    detail.Parameters.AddWithValue("@MaDH", maDH);
                    detail.Parameters.AddWithValue("@MaDT", line.MaDT);
                    detail.Parameters.AddWithValue("@SL", line.SoLuong);
                    detail.Parameters.AddWithValue("@Gia", line.Gia);
                    await detail.ExecuteNonQueryAsync();

                    await using var stock = new SqlCommand(
                        @"UPDATE dbo.DienThoai
                          SET SoLuongTon = SoLuongTon - @SL
                          WHERE MaDT=@MaDT AND SoLuongTon >= @SL",
                        conn, tran);

                    stock.Parameters.AddWithValue("@SL", line.SoLuong);
                    stock.Parameters.AddWithValue("@MaDT", line.MaDT);

                    if (await stock.ExecuteNonQueryAsync() != 1)
                        throw new InvalidOperationException(
                            "Tồn kho vừa thay đổi. Hãy thử lập đơn lại.");
                }

                await tran.CommitAsync();

                TempData["ThongBao"] =
                    $"Tạo đơn hàng #{maDH} thành công. Tổng tiền: {tongTien:N0} đ";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                ModelState.AddModelError("", ex.Message);
                await NapDuLieu(model);
                return View(model);
            }
        }
    }
}
