using System.Data;
using Microsoft.Data.SqlClient;
using QuanLyBanHangDienThoai.Models;

namespace QuanLyBanHangDienThoai.Services;

public class StoreRepository
{
    private readonly string _connectionString;

    public StoreRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("QLBanDienThoai") ?? "";
    }

    public async Task<DashboardData> GetDashboardAsync()
    {
        await using var connection = await OpenConnectionAsync();
        const string sql = """
            SELECT
                (SELECT COUNT(*) FROM dbo.DienThoai WHERE TrangThai = 1) AS SoDienThoai,
                (SELECT COUNT(*) FROM dbo.KhachHang) AS SoKhachHang,
                (SELECT COUNT(*) FROM dbo.DonHang WHERE TrangThai = N'Chờ xử lý') AS SoDonHangMoi,
                (SELECT COALESCE(SUM(TongTien), 0) FROM dbo.DonHang WHERE TrangThai = N'Hoàn thành') AS DoanhThu;
            """;
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        var dashboard = new DashboardData
        {
            SoDienThoai = reader.GetInt32(0),
            SoKhachHang = reader.GetInt32(1),
            SoDonHangMoi = reader.GetInt32(2),
            DoanhThu = reader.GetDecimal(3)
        };
        await reader.CloseAsync();
        dashboard.DonHangMoi = await ReadOrdersAsync(connection,
            """
            SELECT TOP (5) MaDH, NgayDat, TenNguoiNhan, SdtNguoiNhan, DiaChiGiao,
                   TongTien, TrangThai
            FROM dbo.DonHang
            ORDER BY NgayDat DESC, MaDH DESC;
            """);
        return dashboard;
    }

    public async Task<List<KhachHang>> GetCustomersAsync()
    {
        await using var connection = await OpenConnectionAsync();
        const string sql = """
            SELECT MaKH, HoTen, SoDienThoai, Email, DiaChi
            FROM dbo.KhachHang
            ORDER BY MaKH;
            """;
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var customers = new List<KhachHang>();
        while (await reader.ReadAsync())
        {
            customers.Add(new KhachHang
            {
                MaKH = reader.GetInt32(0),
                HoTen = reader.GetString(1),
                SoDienThoai = reader.GetString(2).Trim(),
                Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                DiaChi = reader.IsDBNull(4) ? "" : reader.GetString(4)
            });
        }

        return customers;
    }

    public async Task<KhachHang?> GetCustomerAsync(int id)
    {
        await using var connection = await OpenConnectionAsync();
        const string sql = """
            SELECT MaKH, HoTen, SoDienThoai, Email, DiaChi
            FROM dbo.KhachHang
            WHERE MaKH = @MaKH;
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@MaKH", id);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new KhachHang
        {
            MaKH = reader.GetInt32(0),
            HoTen = reader.GetString(1),
            SoDienThoai = reader.GetString(2).Trim(),
            Email = reader.IsDBNull(3) ? null : reader.GetString(3),
            DiaChi = reader.IsDBNull(4) ? "" : reader.GetString(4)
        };
    }

    public async Task<DeleteCustomerResult> DeleteCustomerAsync(int id)
    {
        await using var connection = await OpenConnectionAsync();
        await using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

        const string customerSql = """
            SELECT MaKH
            FROM dbo.KhachHang WITH (UPDLOCK, HOLDLOCK)
            WHERE MaKH = @MaKH;
            """;
        await using var customerCommand = new SqlCommand(customerSql, connection, transaction);
        customerCommand.Parameters.AddWithValue("@MaKH", id);
        if (await customerCommand.ExecuteScalarAsync() is null)
        {
            await transaction.CommitAsync();
            return DeleteCustomerResult.NotFound;
        }

        const string accountSql = "DELETE FROM dbo.TaiKhoan WHERE MaKH = @MaKH;";
        await using var accountCommand = new SqlCommand(accountSql, connection, transaction);
        accountCommand.Parameters.AddWithValue("@MaKH", id);
        await accountCommand.ExecuteNonQueryAsync();

        const string deleteCustomerSql = "DELETE FROM dbo.KhachHang WHERE MaKH = @MaKH;";
        await using var deleteCustomerCommand = new SqlCommand(deleteCustomerSql, connection, transaction);
        deleteCustomerCommand.Parameters.AddWithValue("@MaKH", id);
        if (await deleteCustomerCommand.ExecuteNonQueryAsync() != 1)
        {
            throw new InvalidOperationException("Customer could not be deleted after validation.");
        }

        await transaction.CommitAsync();
        return DeleteCustomerResult.Deleted;
    }

    public async Task<List<DonHang>> GetOrdersAsync()
    {
        await using var connection = await OpenConnectionAsync();
        return await ReadOrdersAsync(connection,
            """
            SELECT MaDH, NgayDat, TenNguoiNhan, SdtNguoiNhan, DiaChiGiao,
                   TongTien, TrangThai
            FROM dbo.DonHang
            ORDER BY NgayDat DESC, MaDH DESC;
            """);
    }

    public async Task<DonHang?> GetOrderAsync(int id)
    {
        await using var connection = await OpenConnectionAsync();
        const string orderSql = """
            SELECT MaDH, NgayDat, TenNguoiNhan, SdtNguoiNhan, DiaChiGiao,
                   TongTien, TrangThai
            FROM dbo.DonHang
            WHERE MaDH = @MaDH;
            """;
        await using var orderCommand = new SqlCommand(orderSql, connection);
        orderCommand.Parameters.AddWithValue("@MaDH", id);
        await using var reader = await orderCommand.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        var order = ReadOrder(reader);
        await reader.CloseAsync();

        const string detailsSql = """
            SELECT dt.TenDT, ctdh.SoLuong, ctdh.DonGia
            FROM dbo.ChiTietDonHang AS ctdh
            INNER JOIN dbo.DienThoai AS dt ON dt.MaDT = ctdh.MaDT
            WHERE ctdh.MaDH = @MaDH
            ORDER BY dt.TenDT;
            """;
        await using var detailsCommand = new SqlCommand(detailsSql, connection);
        detailsCommand.Parameters.AddWithValue("@MaDH", id);
        await using var detailsReader = await detailsCommand.ExecuteReaderAsync();
        while (await detailsReader.ReadAsync())
        {
            order.ChiTiet.Add(new ChiTietDonHang
            {
                MaDH = id,
                TenSanPham = detailsReader.GetString(0),
                SoLuong = detailsReader.GetInt32(1),
                DonGia = detailsReader.GetDecimal(2)
            });
        }
        return order;
    }

    // Đổi trạng thái đơn hàng theo luồng hợp lệ. Nếu hủy đơn thì hoàn lại tồn kho.
    // Nếu đơn chưa có nhân viên phụ trách thì gán nhân viên đang xử lý (lấy từ tài khoản đăng nhập).
    public async Task<UpdateOrderStatusResult> UpdateOrderStatusAsync(int id, string newStatus, int? maTK)
    {
        await using var connection = await OpenConnectionAsync();
        await using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

        const string currentSql = """
            SELECT TrangThai
            FROM dbo.DonHang WITH (UPDLOCK, HOLDLOCK)
            WHERE MaDH = @MaDH;
            """;
        await using var currentCommand = new SqlCommand(currentSql, connection, transaction);
        currentCommand.Parameters.AddWithValue("@MaDH", id);
        var current = await currentCommand.ExecuteScalarAsync() as string;
        if (current is null)
        {
            await transaction.CommitAsync();
            return UpdateOrderStatusResult.NotFound;
        }

        if (!TrangThaiDonHang.CoTheChuyen(current, newStatus))
        {
            await transaction.CommitAsync();
            return UpdateOrderStatusResult.InvalidTransition;
        }

        const string updateSql = """
            UPDATE dbo.DonHang
            SET TrangThai = @TrangThai,
                MaNV = COALESCE(MaNV, (SELECT MaNV FROM dbo.TaiKhoan WHERE MaTK = @MaTK))
            WHERE MaDH = @MaDH;
            """;
        await using var updateCommand = new SqlCommand(updateSql, connection, transaction);
        updateCommand.Parameters.AddWithValue("@TrangThai", newStatus);
        updateCommand.Parameters.AddWithValue("@MaTK", (object?)maTK ?? DBNull.Value);
        updateCommand.Parameters.AddWithValue("@MaDH", id);
        await updateCommand.ExecuteNonQueryAsync();

        if (newStatus == TrangThaiDonHang.DaHuy)
        {
            const string restoreSql = """
                UPDATE dt
                SET dt.SoLuongTon = dt.SoLuongTon + ct.SoLuong
                FROM dbo.DienThoai AS dt
                INNER JOIN dbo.ChiTietDonHang AS ct ON ct.MaDT = dt.MaDT
                WHERE ct.MaDH = @MaDH;
                """;
            await using var restoreCommand = new SqlCommand(restoreSql, connection, transaction);
            restoreCommand.Parameters.AddWithValue("@MaDH", id);
            await restoreCommand.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
        return UpdateOrderStatusResult.Updated;
    }

    public async Task<List<StoreDienThoai>> GetPhonesAsync()
    {
        await using var connection = await OpenConnectionAsync();
        const string sql = """
            SELECT MaDT, TenDT, TenDM, Gia, SoLuongTon, HinhAnh, ManHinh, RAM, BoNho, Pin, MoTa
            FROM dbo.vw_DienThoai
            WHERE TrangThai = 1
            ORDER BY MaDT;
            """;
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var phones = new List<StoreDienThoai>();
        while (await reader.ReadAsync())
        {
            phones.Add(new StoreDienThoai
            {
                MaDT = reader.GetInt32(0),
                TenDT = reader.GetString(1),
                TenDM = reader.GetString(2),
                Gia = reader.GetDecimal(3),
                SoLuongTon = reader.GetInt32(4),
                HinhAnh = reader.IsDBNull(5) ? null : reader.GetString(5),
                ManHinh = reader.IsDBNull(6) ? null : reader.GetString(6),
                RAM = reader.IsDBNull(7) ? null : reader.GetString(7),
                BoNho = reader.IsDBNull(8) ? null : reader.GetString(8),
                Pin = reader.IsDBNull(9) ? null : reader.GetString(9),
                MoTa = reader.IsDBNull(10) ? null : reader.GetString(10)
            });
        }

        return phones;
    }

    private async Task<SqlConnection> OpenConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "SQL Server connection is not configured. Set ConnectionStrings:QLBanDienThoai.");
        }

        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }

    private static async Task<List<DonHang>> ReadOrdersAsync(SqlConnection connection, string sql)
    {
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var orders = new List<DonHang>();
        while (await reader.ReadAsync())
        {
            orders.Add(ReadOrder(reader));
        }

        return orders;
    }

    private static DonHang ReadOrder(SqlDataReader reader) => new()
    {
        MaDH = reader.GetInt32(0),
        NgayDat = reader.GetDateTime(1),
        TenNguoiNhan = reader.GetString(2),
        SdtNguoiNhan = reader.GetString(3).Trim(),
        DiaChiGiaoHang = reader.GetString(4),
        TongTien = reader.GetDecimal(5),
        TrangThai = reader.GetString(6)
    };
}
