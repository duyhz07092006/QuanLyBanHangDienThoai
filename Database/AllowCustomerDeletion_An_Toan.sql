/*
  Mục đích : cho phép xóa khách hàng mà vẫn giữ lại lịch sử đơn hàng
             (DonHang.MaKH cho phép NULL, khóa ngoại ON DELETE SET NULL).
  An toàn  : KHÔNG xóa database/bảng/dữ liệu. Không đụng tới view.
             Chạy trong 1 transaction: lỗi thì tự hoàn tác. Chạy lại nhiều lần vẫn được.
  Trước khi chạy: sao lưu database (xem hướng dẫn) và đóng ứng dụng đang chạy.
*/
USE QLBanDienThoai;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_DonHang_KhachHang'
      AND parent_object_id = OBJECT_ID(N'dbo.DonHang')
)
    ALTER TABLE dbo.DonHang DROP CONSTRAINT FK_DonHang_KhachHang;

IF COLUMNPROPERTY(OBJECT_ID(N'dbo.DonHang'), N'MaKH', 'AllowsNull') = 0
    ALTER TABLE dbo.DonHang ALTER COLUMN MaKH INT NULL;

ALTER TABLE dbo.DonHang
    ADD CONSTRAINT FK_DonHang_KhachHang
    FOREIGN KEY (MaKH) REFERENCES dbo.KhachHang(MaKH)
    ON DELETE SET NULL;

COMMIT TRANSACTION;
GO

-- Kiểm tra kết quả: MaKH_AllowsNull phải là 1, delete_referential_action_desc phải là SET_NULL
SELECT COLUMNPROPERTY(OBJECT_ID('dbo.DonHang'),'MaKH','AllowsNull') AS MaKH_AllowsNull;
SELECT name, delete_referential_action_desc FROM sys.foreign_keys WHERE name = 'FK_DonHang_KhachHang';
GO
