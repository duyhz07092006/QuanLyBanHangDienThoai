using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using QuanLyBanHangDienThoai.Models;
using QuanLyBanHangDienThoai.Services;

namespace QuanLyBanHangDienThoai.Controllers;

[Authorize(Roles = "Admin,NhanVien")]
public class KhachHangController : Controller
{
    private readonly StoreRepository _store;

    public KhachHangController(StoreRepository store)
    {
        _store = store;
    }

    public async Task<IActionResult> Index() => View(await _store.GetCustomersAsync());

    public async Task<IActionResult> Details(int id)
    {
        var customer = await _store.GetCustomerAsync(id);
        return customer is null ? NotFound() : View(customer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _store.DeleteCustomerAsync(id);
        switch (result)
        {
            case DeleteCustomerResult.Deleted:
                TempData["SuccessMessage"] = "Đã xóa hồ sơ khách hàng và tài khoản liên kết; lịch sử đơn hàng được giữ lại.";
                break;
            case DeleteCustomerResult.NotFound:
                TempData["ErrorMessage"] = "Khách hàng không tồn tại hoặc đã được xóa.";
                break;
            default:
                throw new InvalidOperationException($"Unexpected customer deletion result: {result}.");
        }

        return RedirectToAction(nameof(Index));
    }
}