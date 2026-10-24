using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBanHangDienThoai.Models;

namespace QuanLyBanHangDienThoai.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==================== ĐĂNG NHẬP ====================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("KhachHang"))
                {
                    return RedirectToAction("Index", "CuaHang");
                }

                return RedirectToAction("Index", "Home");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model,
            string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string tenDangNhap = model.TenDangNhap.Trim();

            var taiKhoan = await _context.TaiKhoan
                .AsNoTracking()
                .FirstOrDefaultAsync(t =>
                    t.TenDangNhap == tenDangNhap);

            if (taiKhoan == null || !taiKhoan.TrangThai)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Tài khoản không tồn tại hoặc đã bị khóa.");

                return View(model);
            }

            // Kiểm tra mật khẩu SHA-256 dạng HEX
            string matKhauHash = Convert.ToHexString(
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(model.MatKhau)));

            if (!string.Equals(
                    taiKhoan.MatKhau,
                    matKhauHash,
                    StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Tên đăng nhập hoặc mật khẩu không đúng.");

                return View(model);
            }

            string vaiTro = ChuanHoaVaiTro(taiKhoan.VaiTro);

            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    taiKhoan.MaTK.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    taiKhoan.TenDangNhap),

                new Claim(
                    ClaimTypes.Role,
                    vaiTro)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);

            // Chỉ chuyển về returnUrl nếu đường dẫn hợp lệ
            if (!string.IsNullOrEmpty(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // Khách hàng vào danh sách điện thoại
            if (vaiTro == "KhachHang")
            {
                return RedirectToAction("Index", "CuaHang");
            }

            // Admin và nhân viên vào trang tổng quan
            return RedirectToAction("Index", "Home");
        }

        // ==================== ĐĂNG KÝ ====================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return User.IsInRole("KhachHang")
                    ? RedirectToAction("Index", "CuaHang")
                    : RedirectToAction("Index", "Home");
            }

            return View(new RegisterViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string tenDangNhap = model.TenDangNhap.Trim();

            bool daTonTai = await _context.TaiKhoan
                .AnyAsync(t => t.TenDangNhap == tenDangNhap);

            if (daTonTai)
            {
                ModelState.AddModelError(
                    nameof(model.TenDangNhap),
                    "Tên đăng nhập đã tồn tại.");

                return View(model);
            }

            // Tạo hồ sơ khách hàng
            var khachHang = new KhachHang
            {
                HoTen = model.HoTen.Trim(),
                SoDienThoai = model.SoDienThoai.Trim(),
                Email = string.IsNullOrWhiteSpace(model.Email)
                    ? null
                    : model.Email.Trim(),
                DiaChi = model.DiaChi?.Trim() ?? string.Empty
            };

            // Lưu hồ sơ khách hàng trước để lấy MaKH
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                _context.KhachHang.Add(khachHang);
                await _context.SaveChangesAsync();

                // Dùng cùng định dạng SHA-256 với chức năng đăng nhập
                string matKhauHash = Convert.ToHexString(
                    SHA256.HashData(
                        Encoding.UTF8.GetBytes(model.MatKhau)));

                var taiKhoan = new TaiKhoan
                {
                    TenDangNhap = tenDangNhap,
                    MatKhau = matKhauHash,
                    VaiTro = "KhachHang",
                    TrangThai = true,
                    MaKH = khachHang.MaKH,
                    MaNV = null
                };

                _context.TaiKhoan.Add(taiKhoan);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["ThongBao"] =
                    "Đăng ký thành công! Hãy đăng nhập bằng tài khoản vừa tạo.";

                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "Lỗi đăng ký: " + ex.GetBaseException().Message);

                return View(model);
            }
        }

        // ==================== ĐĂNG XUẤT ====================

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Login", "Account");
        }

        // ==================== TỪ CHỐI TRUY CẬP ====================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ==================== CHUẨN HÓA VAI TRÒ ====================

        private static string ChuanHoaVaiTro(string? vaiTro)
        {
            string role = (vaiTro ?? "").Trim();

            if (role.Equals(
                    "Admin",
                    StringComparison.OrdinalIgnoreCase) ||
                role.Equals(
                    "Quản trị viên",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "Admin";
            }

            if (role.Equals(
                    "NhanVien",
                    StringComparison.OrdinalIgnoreCase) ||
                role.Equals(
                    "Nhân viên",
                    StringComparison.OrdinalIgnoreCase) ||
                role.Equals(
                    "Employee",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "NhanVien"; 
            }

            if (role.Equals(
                    "KhachHang",
                    StringComparison.OrdinalIgnoreCase) ||
                role.Equals(
                    "Khách hàng",
                    StringComparison.OrdinalIgnoreCase) ||
                role.Equals(
                    "Customer",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "KhachHang";
            }

            return role;
        }
    }
}