using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBanHangDienThoai.Models;
using QuanLyBanHangDienThoai.Services;
using QuanLyBanHangDienThoai.ViewModels;

namespace QuanLyBanHangDienThoai.Controllers.Api
{
    // RESTful API quản lý điện thoại
    //   GET    /api/dienthoai          danh sách (tìm kiếm, lọc, sắp xếp, phân trang)  - công khai
    //   GET    /api/dienthoai/{id}     chi tiết                                         - công khai
    //   POST   /api/dienthoai          thêm mới                                         - Admin, NhanVien
    //   PUT    /api/dienthoai/{id}     cập nhật                                         - Admin, NhanVien
    //   DELETE /api/dienthoai/{id}     xóa                                              - Admin
    [ApiController]
    [Route("api/dienthoai")]
    public class DienThoaiApiController : ControllerBase
    {
        private const int KichThuocToiDa = 50;

        private readonly ApplicationDbContext _context;

        public DienThoaiApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<KetQuaPhanTrang<DienThoaiDto>>> GetAll(
            string? tuKhoa, int? maDM, decimal? giaTu, decimal? giaDen,
            string? sapXep, int trang = 1, int kichThuoc = 10)
        {
            if (giaTu.HasValue && giaDen.HasValue && giaTu > giaDen)
                (giaTu, giaDen) = (giaDen, giaTu);

            kichThuoc = Math.Clamp(kichThuoc, 1, KichThuocToiDa);

            var query = _context.DienThoai
                .AsNoTracking()
                .Loc(tuKhoa, maDM, giaTu, giaDen)
                .SapXepTheo(sapXep)
                .Select(d => new DienThoaiDto
                {
                    MaDT = d.MaDT,
                    TenDT = d.TenDT,
                    MaDM = d.MaDM,
                    TenHang = d.DanhMuc != null ? d.DanhMuc.TenDM : null,
                    Gia = d.Gia,
                    SoLuongTon = d.SoLuongTon,
                    HinhAnh = d.HinhAnh,
                    ManHinh = d.ManHinh,
                    RAM = d.RAM,
                    BoNho = d.BoNho,
                    Pin = d.Pin,
                    MoTa = d.MoTa,
                    NgayTao = d.NgayTao,
                    TrangThai = d.TrangThai
                });

            return Ok(await KetQuaPhanTrang<DienThoaiDto>.TaoAsync(query, trang, kichThuoc));
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<DienThoaiDto>> GetById(int id)
        {
            var dt = await _context.DienThoai
                .AsNoTracking()
                .Include(d => d.DanhMuc)
                .FirstOrDefaultAsync(d => d.MaDT == id);

            if (dt == null)
                return NotFound();

            return Ok(ToDto(dt));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,NhanVien")]
        public async Task<ActionResult<DienThoaiDto>> Create(DienThoaiInputDto input)
        {
            input.TenDT = input.TenDT.Trim();

            var loi = await KiemTraNghiepVu(input, idDangSua: null);
            if (loi != null)
                return loi;

            var dt = new DienThoai { NgayTao = DateTime.Now };
            GanDuLieu(dt, input);

            _context.DienThoai.Add(dt);
            await _context.SaveChangesAsync();
            await _context.Entry(dt).Reference(d => d.DanhMuc).LoadAsync();

            return CreatedAtAction(nameof(GetById), new { id = dt.MaDT }, ToDto(dt));
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,NhanVien")]
        public async Task<ActionResult<DienThoaiDto>> Update(int id, DienThoaiInputDto input)
        {
            var dt = await _context.DienThoai
                .Include(d => d.DanhMuc)
                .FirstOrDefaultAsync(d => d.MaDT == id);

            if (dt == null)
                return NotFound();

            input.TenDT = input.TenDT.Trim();

            var loi = await KiemTraNghiepVu(input, idDangSua: id);
            if (loi != null)
                return loi;

            GanDuLieu(dt, input);
            await _context.SaveChangesAsync();
            await _context.Entry(dt).Reference(d => d.DanhMuc).LoadAsync();

            return Ok(ToDto(dt));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var dt = await _context.DienThoai.FirstOrDefaultAsync(d => d.MaDT == id);
            if (dt == null)
                return NotFound();

            // Giống giao diện web: đã có trong đơn hàng thì chuyển sang "ngừng bán"
            if (await _context.ChiTietDonHang.AnyAsync(ct => ct.MaDT == id))
            {
                dt.TrangThai = false;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Điện thoại đã có trong đơn hàng nên không thể xóa; đã chuyển sang trạng thái Ngừng bán.",
                    ngungBan = true
                });
            }

            _context.DienThoai.Remove(dt);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ----- hàm hỗ trợ -----

        private async Task<ActionResult?> KiemTraNghiepVu(DienThoaiInputDto input, int? idDangSua)
        {
            if (!await _context.DanhMuc.AnyAsync(d => d.MaDM == input.MaDM))
                ModelState.AddModelError(nameof(input.MaDM), "Hãng điện thoại không tồn tại.");

            if (await _context.DienThoai.AnyAsync(d =>
                    d.TenDT == input.TenDT &&
                    d.MaDM == input.MaDM &&
                    d.MaDT != (idDangSua ?? 0)))
            {
                ModelState.AddModelError(nameof(input.TenDT),
                    "Điện thoại này đã tồn tại trong hãng đã chọn.");
            }

            return ModelState.IsValid ? null : ValidationProblem(ModelState);
        }

        private static void GanDuLieu(DienThoai dt, DienThoaiInputDto input)
        {
            dt.TenDT = input.TenDT;
            dt.MaDM = input.MaDM;
            dt.Gia = input.Gia;
            dt.SoLuongTon = input.SoLuongTon;
            dt.HinhAnh = input.HinhAnh;
            dt.ManHinh = input.ManHinh;
            dt.RAM = input.RAM;
            dt.BoNho = input.BoNho;
            dt.Pin = input.Pin;
            dt.MoTa = input.MoTa;
            dt.TrangThai = input.TrangThai;
        }

        private static DienThoaiDto ToDto(DienThoai d) => new()
        {
            MaDT = d.MaDT,
            TenDT = d.TenDT,
            MaDM = d.MaDM,
            TenHang = d.DanhMuc?.TenDM,
            Gia = d.Gia,
            SoLuongTon = d.SoLuongTon,
            HinhAnh = d.HinhAnh,
            ManHinh = d.ManHinh,
            RAM = d.RAM,
            BoNho = d.BoNho,
            Pin = d.Pin,
            MoTa = d.MoTa,
            NgayTao = d.NgayTao,
            TrangThai = d.TrangThai
        };
    }
}
