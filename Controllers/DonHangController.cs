using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using QuanLyBanHangDienThoai.Models;
using QuanLyBanHangDienThoai.Services;

namespace QuanLyBanHangDienThoai.Controllers;

[Authorize(Roles = "Admin,NhanVien")]
public class DonHangController : Controller
{
    private readonly StoreRepository _store;

    public DonHangController(StoreRepository store)
    {
        _store = store;
    }

    public async Task<IActionResult> Index() => View(await _store.GetOrdersAsync());

    public async Task<IActionResult> Details(int id)
    {
        var order = await _store.GetOrderAsync(id);
        return order is null ? NotFound() : View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CapNhatTrangThai(int id, string trangThai)
    {
        int? maTK = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var value)
            ? value
            : null;

        var result = await _store.UpdateOrderStatusAsync(id, trangThai ?? "", maTK);
        switch (result)
        {
            case UpdateOrderStatusResult.Updated:
                TempData["SuccessMessage"] = trangThai == TrangThaiDonHang.DaHuy
                    ? "Đã hủy đơn hàng và hoàn lại số lượng vào kho."
                    : $"Đã chuyển đơn hàng sang trạng thái \"{trangThai}\".";
                break;
            case UpdateOrderStatusResult.NotFound:
                return NotFound();
            case UpdateOrderStatusResult.InvalidTransition:
                TempData["ErrorMessage"] = "Không thể chuyển đơn hàng sang trạng thái này (đơn đã kết thúc hoặc sai thứ tự).";
                break;
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
