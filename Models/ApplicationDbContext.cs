
using Microsoft.EntityFrameworkCore;


namespace QuanLyBanHangDienThoai.Models
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<TaiKhoan> TaiKhoan { get; set; }

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<DienThoai> DienThoai { get; set; }
        public DbSet<KhachHang> KhachHang { get; set; }
        public DbSet<DanhMuc> DanhMuc { get; set; }
        public DbSet<DonHang> DonHang { get; set; }
        public DbSet<ChiTietDonHang> ChiTietDonHang { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DienThoai>()
                .HasOne(d => d.DanhMuc)
                .WithMany(dm => dm.DienThoais)
                .HasForeignKey(d => d.MaDM);

            modelBuilder.Entity<ChiTietDonHang>()
                .HasKey(ct => new { ct.MaDH, ct.MaDT });

            modelBuilder.Entity<ChiTietDonHang>()
                .HasOne(ct => ct.DonHang)
                .WithMany(dh => dh.ChiTiet)
                .HasForeignKey(ct => ct.MaDH);

            modelBuilder.Entity<ChiTietDonHang>()
                .HasOne(ct => ct.DienThoai)
                .WithMany()
                .HasForeignKey(ct => ct.MaDT);
        }

    }
}
