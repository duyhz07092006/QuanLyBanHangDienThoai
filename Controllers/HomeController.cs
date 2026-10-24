
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyBanHangDienThoai.Services;
using QuanLyBanHangDienThoai.Models;
using System.Diagnostics;

namespace QuanLyBanHangDienThoai.Controllers
{
    [Authorize(Roles = "Admin,NhanVien,KhachHang")]
    public class HomeController : Controller
    {
        private readonly StoreRepository _store;

        public HomeController(StoreRepository store)
        {
            _store = store;
        }

        public async Task<IActionResult> Index()
        {
            // Khách hàng chuyển thẳng sang danh sách điện thoại
            if (User.IsInRole("KhachHang"))
            {
                return RedirectToAction("Index", "CuaHang");
            }

            // Admin và Nhân viên xem trang tổng quan
            return View(await _store.GetDashboardAsync());
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id
                    ?? HttpContext.TraceIdentifier
            });
        }
    }
}
