using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBanHangDienThoai.Models;

namespace QuanLyBanHangDienThoai.Controllers
{
    // Quản lý hãng điện thoại: chỉ Admin
    [Authorize(Roles = "Admin")]
    public class DanhMucController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DanhMucController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var danhSach = await _context.DanhMuc
                .AsNoTracking()
                .OrderBy(d => d.TenDM)
                .ToListAsync();

            ViewBag.SoDienThoai = await _context.DienThoai
                .GroupBy(d => d.MaDM)
                .Select(g => new { MaDM = g.Key, SoLuong = g.Count() })
                .ToDictionaryAsync(x => x.MaDM, x => x.SoLuong);

            return View(danhSach);
        }

        [HttpGet]
        public IActionResult Create() => View(new DanhMuc());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TenDM,MoTa")] DanhMuc danhMuc)
        {
            danhMuc.TenDM = (danhMuc.TenDM ?? "").Trim();

            if (ModelState.IsValid &&
                await _context.DanhMuc.AnyAsync(d => d.TenDM == danhMuc.TenDM))
            {
                ModelState.AddModelError(nameof(DanhMuc.TenDM), "Tên hãng này đã tồn tại.");
            }

            if (!ModelState.IsValid)
                return View(danhMuc);

            _context.DanhMuc.Add(danhMuc);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã thêm hãng \"{danhMuc.TenDM}\".";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var danhMuc = await _context.DanhMuc.FindAsync(id);
            return danhMuc is null ? NotFound() : View(danhMuc);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaDM,TenDM,MoTa")] DanhMuc danhMuc)
        {
            if (id != danhMuc.MaDM)
                return BadRequest();

            danhMuc.TenDM = (danhMuc.TenDM ?? "").Trim();

            if (ModelState.IsValid &&
                await _context.DanhMuc.AnyAsync(d => d.TenDM == danhMuc.TenDM && d.MaDM != id))
            {
                ModelState.AddModelError(nameof(DanhMuc.TenDM), "Tên hãng này đã tồn tại.");
            }

            if (!ModelState.IsValid)
                return View(danhMuc);

            var banGhi = await _context.DanhMuc.FindAsync(id);
            if (banGhi is null)
                return NotFound();

            banGhi.TenDM = danhMuc.TenDM;
            banGhi.MoTa = string.IsNullOrWhiteSpace(danhMuc.MoTa) ? null : danhMuc.MoTa.Trim();
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã cập nhật hãng \"{banGhi.TenDM}\".";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var danhMuc = await _context.DanhMuc.FindAsync(id);
            if (danhMuc is null)
            {
                TempData["ErrorMessage"] = "Hãng không tồn tại hoặc đã được xóa.";
                return RedirectToAction(nameof(Index));
            }

            if (await _context.DienThoai.AnyAsync(d => d.MaDM == id))
            {
                TempData["ErrorMessage"] =
                    $"Không thể xóa hãng \"{danhMuc.TenDM}\" vì vẫn còn điện thoại thuộc hãng này.";
                return RedirectToAction(nameof(Index));
            }

            _context.DanhMuc.Remove(danhMuc);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xóa hãng \"{danhMuc.TenDM}\".";
            return RedirectToAction(nameof(Index));
        }
    }
}
