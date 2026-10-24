/* Hoàn tác về trạng thái cũ. Chỉ chạy được nếu KHÔNG có đơn hàng nào đang có MaKH = NULL. */
USE QLBanDienThoai;
GO
IF EXISTS (SELECT 1 FROM dbo.DonHang WHERE MaKH IS NULL)
    THROW 50001, N'Còn đơn hàng có MaKH = NULL (khách đã bị xóa). Không thể hoàn tác.', 1;
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;
ALTER TABLE dbo.DonHang DROP CONSTRAINT FK_DonHang_KhachHang;
ALTER TABLE dbo.DonHang ALTER COLUMN MaKH INT NOT NULL;
ALTER TABLE dbo.DonHang ADD CONSTRAINT FK_DonHang_KhachHang
    FOREIGN KEY (MaKH) REFERENCES dbo.KhachHang(MaKH);
COMMIT TRANSACTION;
GO
