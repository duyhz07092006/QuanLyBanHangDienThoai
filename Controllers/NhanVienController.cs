
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using QuanLyBanHangDienThoai.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuanLyBanHangDienThoai.Controllers
{
    [Authorize(Roles = "Admin")]
    public class NhanVienController : Controller
    {
        private readonly string _connectionString;

        public NhanVienController(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("QLBanDienThoai")
                ?? throw new InvalidOperationException(
                    "Chưa cấu hình connection string QLBanDienThoai");
        }

        public async Task<IActionResult> Index()
        {
            var danhSach = new List<NhanVien>();

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var sql = @"SELECT MaNV, HoTen, GioiTinh, NgaySinh,
                               SoDienThoai, Email, DiaChi, ChucVu
                        FROM dbo.NhanVien
                        ORDER BY MaNV DESC";

            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                danhSach.Add(new NhanVien
                {
                    MaNV = reader.GetInt32(0),
                    HoTen = reader.GetString(1),
                    GioiTinh = reader.IsDBNull(2) ? null : reader.GetString(2),
                    NgaySinh = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                    SoDienThoai = reader.GetString(4),
                    Email = reader.IsDBNull(5) ? null : reader.GetString(5),
                    DiaChi = reader.IsDBNull(6) ? null : reader.GetString(6),
                    ChucVu = reader.IsDBNull(7) ? null : reader.GetString(7)
                });
            }

            return View(danhSach);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NhanVien nv)
        {
            if (!ModelState.IsValid)
                return View(nv);

            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                var sql = @"INSERT INTO dbo.NhanVien
                            (HoTen, GioiTinh, NgaySinh, SoDienThoai,
                             Email, DiaChi, ChucVu)
                            VALUES
                            (@HoTen, @GioiTinh, @NgaySinh, @SoDienThoai,
                             @Email, @DiaChi, @ChucVu)";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@HoTen", nv.HoTen);
                cmd.Parameters.AddWithValue("@GioiTinh",
                    (object?)nv.GioiTinh ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@NgaySinh",
                    (object?)nv.NgaySinh ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SoDienThoai", nv.SoDienThoai);
                cmd.Parameters.AddWithValue("@Email",
                    (object?)nv.Email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DiaChi",
                    (object?)nv.DiaChi ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ChucVu",
                    (object?)nv.ChucVu ?? DBNull.Value);

                await cmd.ExecuteNonQueryAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                ModelState.AddModelError("SoDienThoai",
                    "Số điện thoại này đã tồn tại.");
                return View(nv);
            }

        }

        public async Task<IActionResult> Edit(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(
                @"SELECT MaNV, HoTen, GioiTinh, NgaySinh,
                 SoDienThoai, Email, DiaChi, ChucVu
          FROM dbo.NhanVien WHERE MaNV = @id", conn);

            cmd.Parameters.AddWithValue("@id", id);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return NotFound();

            var nv = new NhanVien
            {
                MaNV = reader.GetInt32(0),
                HoTen = reader.GetString(1),
                GioiTinh = reader.IsDBNull(2) ? null : reader.GetString(2),
                NgaySinh = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                SoDienThoai = reader.GetString(4),
                Email = reader.IsDBNull(5) ? null : reader.GetString(5),
                DiaChi = reader.IsDBNull(6) ? null : reader.GetString(6),
                ChucVu = reader.IsDBNull(7) ? null : reader.GetString(7)
            };

            return View(nv);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NhanVien nv)
        {
            if (id != nv.MaNV)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(nv);

            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand(
                    @"UPDATE dbo.NhanVien
              SET HoTen = @HoTen, GioiTinh = @GioiTinh,
                  NgaySinh = @NgaySinh, SoDienThoai = @SDT,
                  Email = @Email, DiaChi = @DiaChi, ChucVu = @ChucVu
              WHERE MaNV = @MaNV", conn);

                cmd.Parameters.AddWithValue("@MaNV", id);
                cmd.Parameters.AddWithValue("@HoTen", nv.HoTen);
                cmd.Parameters.AddWithValue("@GioiTinh",
                    (object?)nv.GioiTinh ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@NgaySinh",
                    (object?)nv.NgaySinh ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SDT", nv.SoDienThoai);
                cmd.Parameters.AddWithValue("@Email",
                    (object?)nv.Email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DiaChi",
                    (object?)nv.DiaChi ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ChucVu",
                    (object?)nv.ChucVu ?? DBNull.Value);

                await cmd.ExecuteNonQueryAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                ModelState.AddModelError("SoDienThoai",
                    "Số điện thoại đã được sử dụng.");
                return View(nv);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            try
            {
                using var cmd = new SqlCommand(
                    "DELETE FROM dbo.NhanVien WHERE MaNV = @id", conn);

                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                TempData["Loi"] =
                    "Không thể xóa nhân viên vì đang được đơn hàng hoặc tài khoản tham chiếu.";
            }

            return RedirectToAction(nameof(Index));
        }

    }
}
