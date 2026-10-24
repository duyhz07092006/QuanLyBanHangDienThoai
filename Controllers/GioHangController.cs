using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBanHangDienThoai.Models;
using QuanLyBanHangDienThoai.Services;
using QuanLyBanHangDienThoai.ViewModels;

namespace QuanLyBanHangDienThoai.Controllers
{
    [Authorize(Roles = "KhachHang")]
    public class GioHangController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GioHangController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var gioHang = HttpContext.Session.LayGioHang();
            return View(gioHang);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Them(int id, int soLuong = 1)
        {
            if (soLuong < 1)
                soLuong = 1;

            var dienThoai = await _context.DienThoai
                .FirstOrDefaultAsync(d =>
                    d.MaDT == id &&
                    d.TrangThai &&
                    d.SoLuongTon > 0);

            if (dienThoai == null)
            {
                TempData["Loi"] = "Điện thoại không tồn tại hoặc đã hết hàng.";
                return RedirectToAction("Index", "DienThoai");
            }

            var gioHang = HttpContext.Session.LayGioHang();
            var sanPham = gioHang.FirstOrDefault(x => x.MaDT == id);

            int soLuongHienTai = sanPham?.SoLuong ?? 0;

            if (soLuongHienTai + soLuong > dienThoai.SoLuongTon)
            {
                TempData["Loi"] =
                    $"Sản phẩm chỉ còn {dienThoai.SoLuongTon} chiếc trong kho.";
                return RedirectToAction("Index");
            }

            if (sanPham == null)
            {
                gioHang.Add(new GioHangItem
                {
                    MaDT = dienThoai.MaDT,
                    TenDT = dienThoai.TenDT,
                    HinhAnh = dienThoai.HinhAnh,
                    DonGia = dienThoai.Gia,
                    SoLuong = soLuong
                });
            }
            else
            {
                sanPham.SoLuong += soLuong;
            }

            HttpContext.Session.LuuGioHang(gioHang);

            TempData["ThongBao"] = "Đã thêm điện thoại vào giỏ hàng.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhat(int id, int soLuong)
        {
            var gioHang = HttpContext.Session.LayGioHang();
            var sanPham = gioHang.FirstOrDefault(x => x.MaDT == id);

            if (sanPham == null)
                return RedirectToAction("Index");

            if (soLuong <= 0)
            {
                gioHang.Remove(sanPham);
            }
            else
            {
                var dienThoai = await _context.DienThoai
                    .FirstOrDefaultAsync(d => d.MaDT == id && d.TrangThai);

                if (dienThoai == null || soLuong > dienThoai.SoLuongTon)
                {
                    TempData["Loi"] = "Số lượng vượt quá số lượng tồn kho.";
                    return RedirectToAction("Index");
                }

                sanPham.SoLuong = soLuong;
            }

            HttpContext.Session.LuuGioHang(gioHang);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Xoa(int id)
        {
            var gioHang = HttpContext.Session.LayGioHang();
            gioHang.RemoveAll(x => x.MaDT == id);
            HttpContext.Session.LuuGioHang(gioHang);

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult DatHang()
        {
            var gioHang = HttpContext.Session.LayGioHang();

            if (gioHang.Count == 0)
            {
                TempData["Loi"] = "Giỏ hàng đang trống.";
                return RedirectToAction(nameof(Index));
            }

            return View(new DatHangViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XacNhanDatHang(
            DatHangViewModel model)
        {
            var gioHang = HttpContext.Session.LayGioHang();

            if (gioHang.Count == 0)
            {
                TempData["Loi"] = "Giỏ hàng đang trống.";
                return RedirectToAction(nameof(Index));
            }

            if (model.PhuongThucTT != "COD" &&
                model.PhuongThucTT != "Chuyển khoản")
            {
                ModelState.AddModelError(
                    nameof(model.PhuongThucTT),
                    "Phương thức thanh toán không hợp lệ.");
            }

            if (!ModelState.IsValid)
                return View("DatHang", model);

            var maTKText = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(maTKText, out int maTK))
                return Challenge();

            var taiKhoan = await _context.TaiKhoan
                .FirstOrDefaultAsync(t =>
                    t.MaTK == maTK && t.TrangThai);

            if (taiKhoan == null || taiKhoan.MaKH == null)
            {
                TempData["Loi"] =
                    "Không tìm thấy hồ sơ khách hàng của tài khoản.";
                return RedirectToAction("Index", "DienThoai");
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var donHang = new DonHang
                {
                    MaKH = taiKhoan.MaKH.Value,
                    MaNV = null,
                    NgayDat = DateTime.Now,
                    TenNguoiNhan = model.TenNguoiNhan.Trim(),
                    SdtNguoiNhan = model.SdtNguoiNhan.Trim(),
                    DiaChiGiaoHang = model.DiaChiGiao.Trim(),
                    PhuongThucTT = model.PhuongThucTT,
                    TongTien = 0,
                    TrangThai = "Chờ xử lý",
                    GhiChu = string.IsNullOrWhiteSpace(model.GhiChu)
                        ? null
                        : model.GhiChu.Trim()
                };

                decimal tongTien = 0;

                foreach (var item in gioHang)
                {
                    if (item.SoLuong <= 0)
                    {
                        await transaction.RollbackAsync();
                        TempData["Loi"] =
                            "Số lượng sản phẩm không hợp lệ.";
                        return RedirectToAction(nameof(Index));
                    }

                    var dienThoai = await _context.DienThoai
                        .FirstOrDefaultAsync(d =>
                            d.MaDT == item.MaDT && d.TrangThai);

                    if (dienThoai == null ||
                        dienThoai.SoLuongTon < item.SoLuong)
                    {
                        await transaction.RollbackAsync();
                        TempData["Loi"] =
                            $"Sản phẩm {item.TenDT} không còn đủ hàng.";
                        return RedirectToAction(nameof(Index));
                    }

                    decimal donGia = dienThoai.Gia;
                    tongTien += donGia * item.SoLuong;

                    donHang.ChiTiet.Add(new ChiTietDonHang
                    {
                        MaDT = dienThoai.MaDT,
                        SoLuong = item.SoLuong,
                        DonGia = donGia
                    });

                    dienThoai.SoLuongTon -= item.SoLuong;
                }

                donHang.TongTien = tongTien;

                _context.DonHang.Add(donHang);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                HttpContext.Session.LuuGioHang(
                    new List<GioHangItem>());

                TempData["ThongBao"] =
                    $"Đặt hàng thành công! Mã đơn hàng: {donHang.MaDH}";

                return RedirectToAction("Index", "DienThoai");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                string loi = ex.GetBaseException().Message;

                ModelState.AddModelError(
                    string.Empty,
                    "Lỗi khi lưu đơn hàng: " + loi
                );

                return View("DatHang", model);
            }
        }

    }
}