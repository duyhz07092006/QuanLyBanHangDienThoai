using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyBanHangDienThoai.Services;

namespace QuanLyBanHangDienThoai.Controllers;


[AllowAnonymous]
public class CuaHangController : Controller
{
    private readonly StoreRepository _store;

    public CuaHangController(StoreRepository store)
    {
        _store = store;
    }

    public async Task<IActionResult> Index() => View(await _store.GetPhonesAsync());
}
