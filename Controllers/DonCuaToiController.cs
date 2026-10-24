using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBanHangDienThoai.Models;

namespace QuanLyBanHangDienThoai.Controllers
{
    // Khách hàng xem và hủy đơn của CHÍNH MÌNH
    [Authorize(Roles = "KhachHang")]
    public class DonCuaToiController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DonCuaToiController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var maKH = await LayMaKHAsync();
            if (maKH is null)
                return Challenge();

            var donHang = await _context.DonHang
                .AsNoTracking()
                .Where(d => d.MaKH == maKH)
                .OrderByDescending(d => d.NgayDat)
                .ThenByDescending(d => d.MaDH)
                .ToListAsync();

            return View(donHang);
        }

        public async Task<IActionResult> Details(int id)
        {
            var maKH = await LayMaKHAsync();
            if (maKH is null)
                return Challenge();

            var donHang = await _context.DonHang
                .AsNoTracking()
                .Include(d => d.ChiTiet)
                    .ThenInclude(ct => ct.DienThoai)
                .FirstOrDefaultAsync(d => d.MaDH == id && d.MaKH == maKH);

            return donHang is null ? NotFound() : View(donHang);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Huy(int id)
        {
            var maKH = await LayMaKHAsync();
            if (maKH is null)
                return Challenge();

            var chiTiet = await _context.ChiTietDonHang
                .AsNoTracking()
                .Where(ct => ct.MaDH == id && ct.DonHang!.MaKH == maKH)
                .Select(ct => new { ct.MaDT, ct.SoLuong })
                .ToListAsync();

            await using var transaction = await _context.Database.BeginTransactionAsync();

            // Chỉ hủy được đơn của chính mình và còn ở trạng thái "Chờ xử lý"
            int soDongDoi = await _context.DonHang
                .Where(d => d.MaDH == id
                         && d.MaKH == maKH
                         && d.TrangThai == TrangThaiDonHang.ChoXuLy)
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.TrangThai, TrangThaiDonHang.DaHuy));

            if (soDongDoi != 1)
            {
                await transaction.RollbackAsync();
                TempData["Loi"] = "Không thể hủy đơn hàng này (đơn không tồn tại hoặc đã được cửa hàng xử lý).";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Hoàn lại số lượng vào kho
            foreach (var ct in chiTiet)
            {
                int maDT = ct.MaDT;
                int soLuong = ct.SoLuong;
                await _context.DienThoai
                    .Where(d => d.MaDT == maDT)
                    .ExecuteUpdateAsync(s => s.SetProperty(d => d.SoLuongTon, d => d.SoLuongTon + soLuong));
            }

            await transaction.CommitAsync();

            TempData["ThongBao"] = "Đã hủy đơn hàng.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // MaKH của tài khoản đang đăng nhập (null nếu không có hồ sơ khách hàng)
        private async Task<int?> LayMaKHAsync()
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int maTK))
                return null;

            return await _context.TaiKhoan
                .AsNoTracking()
                .Where(t => t.MaTK == maTK && t.TrangThai)
                .Select(t => t.MaKH)
                .FirstOrDefaultAsync();
        }
    }
}
