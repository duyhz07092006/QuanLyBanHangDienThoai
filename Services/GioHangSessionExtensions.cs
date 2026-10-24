using System.Text.Json;
using Microsoft.AspNetCore.Http;
using QuanLyBanHangDienThoai.Models;

namespace QuanLyBanHangDienThoai.Services
{
    public static class GioHangSessionExtensions
    {
        private const string Key = "GioHang";

        public static List<GioHangItem> LayGioHang(this ISession session)
        {
            var json = session.GetString(Key);

            if (string.IsNullOrEmpty(json))
            {
                return new List<GioHangItem>();
            }

            return JsonSerializer.Deserialize<List<GioHangItem>>(json)
                   ?? new List<GioHangItem>();
        }

        public static void LuuGioHang(
            this ISession session,
            List<GioHangItem> gioHang)
        {
            session.SetString(Key, JsonSerializer.Serialize(gioHang));
        }
    }
}