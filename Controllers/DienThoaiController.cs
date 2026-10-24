using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBanHangDienThoai.Models;
using QuanLyBanHangDienThoai.Services;
using QuanLyBanHangDienThoai.ViewModels;

namespace QuanLyBanHangDienThoai.Controllers
{
    [Authorize]
    public class DienThoaiController : Controller
    {
        private const int KichThuocTrang = 8;

        private readonly ApplicationDbContext _context;

        public DienThoaiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Danh sách + tìm kiếm + lọc theo hãng/giá + sắp xếp + phân trang
        // Mọi tài khoản đã đăng nhập đều xem được
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] DienThoaiLocViewModel loc)
        {
            var vm = new DienThoaiIndexViewModel { Loc = loc };

            // Người dùng nhập "từ" lớn hơn "đến" thì tự đảo lại và báo cho họ biết
            if (loc.GiaTu.HasValue && loc.GiaDen.HasValue && loc.GiaTu > loc.GiaDen)
            {
                (loc.GiaTu, loc.GiaDen) = (loc.GiaDen, loc.GiaTu);
                vm.CanhBao = "Khoảng giá nhập ngược nên hệ thống đã đảo lại.";
            }

            var query = _context.DienThoai
                .AsNoTracking()
                .Include(d => d.DanhMuc)
                .Loc(loc.TuKhoa, loc.MaDM, loc.GiaTu, loc.GiaDen)
                .SapXepTheo(loc.SapXep);

            vm.KetQua = await KetQuaPhanTrang<DienThoai>
                .TaoAsync(query, loc.Trang, KichThuocTrang);
            loc.Trang = vm.KetQua.TrangHienTai;

            vm.DanhMuc = await _context.DanhMuc
                .AsNoTracking()
                .OrderBy(d => d.TenDM)
                .ToListAsync();

            return View(vm);
        }

        // GET: DienThoai/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var dienThoai = await _context.DienThoai
                .AsNoTracking()
                .Include(d => d.DanhMuc)
                .FirstOrDefaultAsync(d => d.MaDT == id);

            if (dienThoai == null)
                return NotFound();

            return View(dienThoai);
        }

        // Hiển thị form thêm: chỉ Admin và Nhân viên
        [HttpGet]
        [Authorize(Roles = "Admin,NhanVien")]
        public async Task<IActionResult> Create()
        {
            await NapDanhMuc();
            // Điện thoại mới mặc định ở trạng thái "Đang bán"
            return View(new DienThoai { TrangThai = true });
        }

        // Lưu điện thoại mới: chỉ Admin và Nhân viên
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,NhanVien")]
        public async Task<IActionResult> Create(
            [Bind("TenDT,MaDM,Gia,SoLuongTon,HinhAnh,ManHinh,RAM,BoNho,Pin,MoTa,TrangThai")]
            DienThoai dienThoai)
        {
            await KiemTraNghiepVu(dienThoai, idDangSua: null);

            if (!ModelState.IsValid)
            {
                await NapDanhMuc();
                return View(dienThoai);
            }

            dienThoai.NgayTao = DateTime.Now;
            _context.DienThoai.Add(dienThoai);
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = $"Đã thêm điện thoại \"{dienThoai.TenDT}\".";
            return RedirectToAction(nameof(Index));
        }

        // Hiển thị form sửa: chỉ Admin và Nhân viên
        [HttpGet]
        [Authorize(Roles = "Admin,NhanVien")]
        public async Task<IActionResult> Edit(int id)
        {
            var dienThoai = await _context.DienThoai
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.MaDT == id);

            if (dienThoai == null)
                return NotFound();

            await NapDanhMuc();
            return View(dienThoai);
        }

        // Lưu thông tin đã sửa: chỉ Admin và Nhân viên
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,NhanVien")]
        public async Task<IActionResult> Edit(int id, DienThoai dienThoai)
        {
            if (id != dienThoai.MaDT)
                return BadRequest();

            await KiemTraNghiepVu(dienThoai, idDangSua: id);

            if (!ModelState.IsValid)
            {
                await NapDanhMuc();
                return View(dienThoai);
            }

            var banGhi = await _context.DienThoai
                .FirstOrDefaultAsync(d => d.MaDT == id);

            if (banGhi == null)
                return NotFound();

            banGhi.TenDT = dienThoai.TenDT;
            banGhi.MaDM = dienThoai.MaDM;
            banGhi.Gia = dienThoai.Gia;
            banGhi.SoLuongTon = dienThoai.SoLuongTon;
            banGhi.HinhAnh = dienThoai.HinhAnh;
            banGhi.ManHinh = dienThoai.ManHinh;
            banGhi.RAM = dienThoai.RAM;
            banGhi.BoNho = dienThoai.BoNho;
            banGhi.Pin = dienThoai.Pin;
            banGhi.MoTa = dienThoai.MoTa;
            banGhi.TrangThai = dienThoai.TrangThai;

            await _context.SaveChangesAsync();

            TempData["ThongBao"] = $"Đã cập nhật điện thoại \"{banGhi.TenDT}\".";
            return RedirectToAction(nameof(Index));
        }

        // Xác nhận xóa: chỉ Admin
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var dienThoai = await _context.DienThoai
                .AsNoTracking()
                .Include(d => d.DanhMuc)
                .FirstOrDefaultAsync(d => d.MaDT == id);

            if (dienThoai == null)
                return NotFound();

            return View(dienThoai);
        }

        // Thực hiện xóa: chỉ Admin
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var dienThoai = await _context.DienThoai
                .FirstOrDefaultAsync(d => d.MaDT == id);

            if (dienThoai == null)
                return NotFound();

            // Điện thoại đã nằm trong đơn hàng thì không xóa được (khóa ngoại),
            // chuyển sang "ngừng bán" để giữ lịch sử đơn hàng.
            bool daCoTrongDonHang = await _context.ChiTietDonHang
                .AnyAsync(ct => ct.MaDT == id);

            if (daCoTrongDonHang)
            {
                dienThoai.TrangThai = false;
                await _context.SaveChangesAsync();

                TempData["ThongBao"] =
                    $"\"{dienThoai.TenDT}\" đã có trong đơn hàng nên không thể xóa; hệ thống chuyển sang trạng thái Ngừng bán.";
                return RedirectToAction(nameof(Index));
            }

            _context.DienThoai.Remove(dienThoai);
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = $"Đã xóa điện thoại \"{dienThoai.TenDT}\".";
            return RedirectToAction(nameof(Index));
        }

        // Kiểm tra các ràng buộc mà DataAnnotations không làm được (cần truy vấn DB)
        private async Task KiemTraNghiepVu(DienThoai dt, int? idDangSua)
        {
            dt.TenDT = (dt.TenDT ?? "").Trim();

            if (dt.MaDM > 0 &&
                !await _context.DanhMuc.AnyAsync(d => d.MaDM == dt.MaDM))
            {
                ModelState.AddModelError(nameof(DienThoai.MaDM),
                    "Hãng điện thoại không tồn tại.");
            }

            if (dt.TenDT.Length >= 2 &&
                await _context.DienThoai.AnyAsync(d =>
                    d.TenDT == dt.TenDT &&
                    d.MaDM == dt.MaDM &&
                    d.MaDT != (idDangSua ?? 0)))
            {
                ModelState.AddModelError(nameof(DienThoai.TenDT),
                    "Điện thoại này đã tồn tại trong hãng đã chọn.");
            }
        }

        private async Task NapDanhMuc()
        {
            ViewBag.DanhMuc = await _context.DanhMuc
                .AsNoTracking()
                .OrderBy(d => d.TenDM)
                .ToListAsync();
        }
    }
}
