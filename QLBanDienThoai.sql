/*
    Đề tài   : Website Quản lý bán điện thoại
    Database : QLBanDienThoai
    Hệ QTCSDL: Microsoft SQL Server 2022

    Lớp người dùng:
      - Khách hàng : đăng ký, đăng nhập, xem/tìm điện thoại, giỏ hàng (lưu trong Session),
                     đặt hàng online, xem và hủy đơn của mình
      - Nhân viên  : quản lý khách hàng, lập đơn tại quầy, xử lý đơn hàng
      - Admin      : toàn quyền, thêm quản lý điện thoại, hãng, nhân viên, tài khoản

    Script build database từ đầu: xóa database cũ (nếu có), tạo bảng và dữ liệu mẫu.
*/

USE master;
GO

IF DB_ID(N'QLBanDienThoai') IS NOT NULL
BEGIN
    ALTER DATABASE QLBanDienThoai SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE QLBanDienThoai;
END
GO

-- Vietnamese_100_CI_AI: so sánh tiếng Việt không phân biệt hoa/thường và dấu
-- (tìm "iphone" ra "iPhone", tìm "dien thoai" ra "điện thoại")
CREATE DATABASE QLBanDienThoai COLLATE Vietnamese_100_CI_AI;
GO

USE QLBanDienThoai;
GO

/* ============================== BẢNG ============================== */

-- Hãng điện thoại
CREATE TABLE DanhMuc (
    MaDM    INT IDENTITY(1,1) PRIMARY KEY,
    TenDM   NVARCHAR(100) NOT NULL UNIQUE,
    MoTa    NVARCHAR(500) NULL
);
GO

CREATE TABLE DienThoai (
    MaDT        INT IDENTITY(1,1) PRIMARY KEY,
    TenDT       NVARCHAR(200) NOT NULL,
    MaDM        INT           NOT NULL,
    Gia         DECIMAL(18,0) NOT NULL CHECK (Gia >= 0),
    SoLuongTon  INT           NOT NULL DEFAULT 0 CHECK (SoLuongTon >= 0),
    HinhAnh     NVARCHAR(255) NULL,
    ManHinh     NVARCHAR(100) NULL,
    RAM         NVARCHAR(20)  NULL,
    BoNho       NVARCHAR(20)  NULL,
    Pin         NVARCHAR(50)  NULL,
    MoTa        NVARCHAR(MAX) NULL,
    NgayTao     DATETIME      NOT NULL DEFAULT GETDATE(),
    TrangThai   BIT           NOT NULL DEFAULT 1,   -- 1: Đang bán, 0: Ngừng bán (ẩn khỏi trang khách hàng)
    CONSTRAINT FK_DienThoai_DanhMuc FOREIGN KEY (MaDM) REFERENCES DanhMuc(MaDM)
);
GO

CREATE TABLE NhanVien (
    MaNV        INT IDENTITY(1,1) PRIMARY KEY,
    HoTen       NVARCHAR(100) NOT NULL,
    GioiTinh    NVARCHAR(10)  NULL CHECK (GioiTinh IN (N'Nam', N'Nữ')),
    NgaySinh    DATE          NULL,
    SoDienThoai VARCHAR(11)   NOT NULL UNIQUE,
    Email       VARCHAR(100)  NULL,
    DiaChi      NVARCHAR(255) NULL,
    ChucVu      NVARCHAR(50)  NULL
);
GO

-- Khách mua tại quầy có thể chưa có tài khoản; khách đăng ký online sẽ có thêm tài khoản
CREATE TABLE KhachHang (
    MaKH        INT IDENTITY(1,1) PRIMARY KEY,
    HoTen       NVARCHAR(100) NOT NULL,
    SoDienThoai VARCHAR(11)   NOT NULL UNIQUE,
    Email       VARCHAR(100)  NULL,
    DiaChi      NVARCHAR(255) NULL,
    NgayTao     DATETIME      NOT NULL DEFAULT GETDATE()
);
GO

-- Tài khoản đăng nhập cho cả 3 lớp người dùng
--   Admin, NhanVien : gắn với một nhân viên (MaNV), MaKH để trống
--   KhachHang       : gắn với một khách hàng (MaKH), MaNV để trống
-- MatKhau lưu mã băm SHA-256 dạng chuỗi hex 64 ký tự in hoa (không lưu mật khẩu gốc)
CREATE TABLE TaiKhoan (
    MaTK        INT IDENTITY(1,1) PRIMARY KEY,
    TenDangNhap VARCHAR(50)  NOT NULL UNIQUE,
    MatKhau     VARCHAR(64)  NOT NULL,
    VaiTro      VARCHAR(20)  NOT NULL CHECK (VaiTro IN ('Admin', 'NhanVien', 'KhachHang')),
    MaNV        INT          NULL,
    MaKH        INT          NULL,
    TrangThai   BIT          NOT NULL DEFAULT 1,   -- 1: Hoạt động, 0: Bị khóa
    NgayTao     DATETIME     NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_TaiKhoan_NhanVien  FOREIGN KEY (MaNV) REFERENCES NhanVien(MaNV),
    CONSTRAINT FK_TaiKhoan_KhachHang FOREIGN KEY (MaKH) REFERENCES KhachHang(MaKH),
    CONSTRAINT CK_TaiKhoan_DoiTuong CHECK (
        (VaiTro IN ('Admin', 'NhanVien') AND MaNV IS NOT NULL AND MaKH IS NULL)
     OR (VaiTro = 'KhachHang'           AND MaKH IS NOT NULL AND MaNV IS NULL)
    )
);
GO

-- Mỗi nhân viên / khách hàng có tối đa 1 tài khoản (bỏ qua các dòng để trống)
CREATE UNIQUE INDEX UX_TaiKhoan_MaNV ON TaiKhoan(MaNV) WHERE MaNV IS NOT NULL;
CREATE UNIQUE INDEX UX_TaiKhoan_MaKH ON TaiKhoan(MaKH) WHERE MaKH IS NOT NULL;
GO

-- Đơn hàng có 2 nguồn:
--   Online : khách tự đặt trên website, MaNV để trống đến khi nhân viên nhận xử lý
--   Tại quầy: nhân viên lập đơn, MaNV là nhân viên đó
CREATE TABLE DonHang (
    MaDH          INT IDENTITY(1,1) PRIMARY KEY,
    MaKH          INT           NOT NULL,
    MaNV          INT           NULL,          -- nhân viên lập hoặc xử lý đơn
    NgayDat       DATETIME      NOT NULL DEFAULT GETDATE(),
    TenNguoiNhan  NVARCHAR(100) NOT NULL,
    SdtNguoiNhan  VARCHAR(11)   NOT NULL,
    DiaChiGiao    NVARCHAR(255) NOT NULL,
    PhuongThucTT  NVARCHAR(20)  NOT NULL DEFAULT N'COD'
                  CHECK (PhuongThucTT IN (N'COD', N'Chuyển khoản', N'Tiền mặt')),
    TongTien      DECIMAL(18,0) NOT NULL DEFAULT 0 CHECK (TongTien >= 0),
    TrangThai     NVARCHAR(20)  NOT NULL DEFAULT N'Chờ xử lý'
                  CHECK (TrangThai IN (N'Chờ xử lý', N'Đang giao', N'Hoàn thành', N'Đã hủy')),
    GhiChu        NVARCHAR(500) NULL,
    CONSTRAINT FK_DonHang_KhachHang FOREIGN KEY (MaKH) REFERENCES KhachHang(MaKH),
    CONSTRAINT FK_DonHang_NhanVien  FOREIGN KEY (MaNV) REFERENCES NhanVien(MaNV)
);
GO

-- Khóa chính gồm 2 cột: một đơn hàng không chứa trùng một điện thoại
CREATE TABLE ChiTietDonHang (
    MaDH    INT           NOT NULL,
    MaDT    INT           NOT NULL,
    SoLuong INT           NOT NULL CHECK (SoLuong > 0),
    DonGia  DECIMAL(18,0) NOT NULL CHECK (DonGia >= 0),   -- giá tại thời điểm bán
    CONSTRAINT PK_ChiTietDonHang PRIMARY KEY (MaDH, MaDT),
    CONSTRAINT FK_CTDH_DonHang   FOREIGN KEY (MaDH) REFERENCES DonHang(MaDH) ON DELETE CASCADE,
    CONSTRAINT FK_CTDH_DienThoai FOREIGN KEY (MaDT) REFERENCES DienThoai(MaDT)
);
GO

/* ============================== INDEX ============================== */

-- Index giúp tìm kiếm, lọc và JOIN nhanh hơn trên các cột hay dùng
CREATE INDEX IX_DienThoai_MaDM    ON DienThoai(MaDM);
CREATE INDEX IX_DienThoai_Gia     ON DienThoai(Gia);
CREATE INDEX IX_DonHang_MaKH      ON DonHang(MaKH);
CREATE INDEX IX_DonHang_MaNV      ON DonHang(MaNV);
CREATE INDEX IX_DonHang_NgayDat   ON DonHang(NgayDat);
CREATE INDEX IX_DonHang_TrangThai ON DonHang(TrangThai);
CREATE INDEX IX_CTDH_MaDT         ON ChiTietDonHang(MaDT);
GO

/* ============================== VIEW =============================== */

-- Danh sách điện thoại kèm tên hãng
CREATE VIEW vw_DienThoai
AS
SELECT dt.MaDT, dt.TenDT, dt.MaDM, dm.TenDM, dt.Gia, dt.SoLuongTon, dt.HinhAnh,
       dt.ManHinh, dt.RAM, dt.BoNho, dt.Pin, dt.MoTa, dt.NgayTao, dt.TrangThai
FROM   DienThoai dt
JOIN   DanhMuc   dm ON dm.MaDM = dt.MaDM;
GO

-- Danh sách đơn hàng kèm tên khách hàng và nhân viên (đơn online chưa xử lý thì chưa có nhân viên)
CREATE VIEW vw_DonHang
AS
SELECT dh.MaDH, dh.NgayDat, dh.TongTien, dh.TrangThai, dh.PhuongThucTT,
       dh.TenNguoiNhan, dh.SdtNguoiNhan, dh.DiaChiGiao, dh.GhiChu,
       dh.MaKH, kh.HoTen AS TenKhachHang,
       dh.MaNV, nv.HoTen AS TenNhanVien
FROM      DonHang   dh
JOIN      KhachHang kh ON kh.MaKH = dh.MaKH
LEFT JOIN NhanVien  nv ON nv.MaNV = dh.MaNV;
GO

/* ============================ DỮ LIỆU MẪU ============================ */

INSERT INTO DanhMuc (TenDM, MoTa) VALUES
(N'Apple',   N'Điện thoại iPhone'),
(N'Samsung', N'Điện thoại Samsung Galaxy'),
(N'Xiaomi',  N'Điện thoại Xiaomi, Redmi'),
(N'OPPO',    N'Điện thoại OPPO'),
(N'Vivo',    N'Điện thoại Vivo'),
(N'Realme',  N'Điện thoại Realme');
GO

-- Số lượng tồn đã trừ các máy bán trong đơn hàng mẫu (trừ đơn đã hủy)
INSERT INTO DienThoai (TenDT, MaDM, Gia, SoLuongTon, HinhAnh, ManHinh, RAM, BoNho, Pin, MoTa) VALUES
(N'iPhone 15 128GB',          1, 19990000, 19, N'iphone15.jpg',        N'6.1 inch OLED',    N'6GB',  N'128GB', N'3349 mAh', N'Thiết kế Dynamic Island, cổng USB-C'),
(N'iPhone 15 Pro Max 256GB',  1, 29990000, 10, N'iphone15promax.jpg',  N'6.7 inch OLED',    N'8GB',  N'256GB', N'4422 mAh', N'Khung titan, camera zoom 5x'),
(N'iPhone 14 128GB',          1, 16990000, 15, N'iphone14.jpg',        N'6.1 inch OLED',    N'6GB',  N'128GB', N'3279 mAh', N'Chip A15 Bionic'),
(N'Samsung Galaxy S24 Ultra', 2, 27990000, 7,  N'galaxys24ultra.jpg',  N'6.8 inch AMOLED',  N'12GB', N'256GB', N'5000 mAh', N'Bút S Pen, camera 200MP'),
(N'Samsung Galaxy A55 5G',    2, 9490000,  24, N'galaxya55.jpg',       N'6.6 inch AMOLED',  N'8GB',  N'128GB', N'5000 mAh', N'Kháng nước IP67'),
(N'Samsung Galaxy Z Flip6',   2, 24990000, 5,  N'zflip6.jpg',          N'6.7 inch AMOLED',  N'12GB', N'256GB', N'4000 mAh', N'Điện thoại gập vỏ sò'),
(N'Xiaomi 14',                3, 19990000, 12, N'xiaomi14.jpg',        N'6.36 inch AMOLED', N'12GB', N'256GB', N'4610 mAh', N'Camera Leica'),
(N'Xiaomi Redmi Note 13',     3, 4890000,  38, N'redminote13.jpg',     N'6.67 inch AMOLED', N'8GB',  N'128GB', N'5000 mAh', N'Giá rẻ, màn hình 120Hz'),
(N'Xiaomi Redmi 13C',         3, 2990000,  49, N'redmi13c.jpg',        N'6.74 inch IPS',    N'4GB',  N'128GB', N'5000 mAh', N'Phổ thông'),
(N'OPPO Reno11 F 5G',         4, 8990000,  18, N'reno11f.jpg',         N'6.7 inch AMOLED',  N'8GB',  N'256GB', N'5000 mAh', N'Sạc nhanh 67W'),
(N'OPPO A79 5G',              4, 6490000,  22, N'oppoa79.jpg',         N'6.72 inch IPS',    N'8GB',  N'256GB', N'5000 mAh', N'Pin trâu, 5G'),
(N'Vivo V30e',                5, 9490000,  14, N'vivov30e.jpg',        N'6.78 inch AMOLED', N'8GB',  N'256GB', N'5500 mAh', N'Thiết kế mỏng nhẹ'),
(N'Vivo Y36',                 5, 5290000,  20, N'vivoy36.jpg',         N'6.64 inch IPS',    N'8GB',  N'128GB', N'5000 mAh', N'Sạc nhanh 44W'),
(N'Realme C67',               6, 4990000,  29, N'realmec67.jpg',       N'6.72 inch IPS',    N'8GB',  N'128GB', N'5000 mAh', N'Camera 108MP'),
(N'Realme 12 Pro+ 5G',        6, 11990000, 9,  N'realme12proplus.jpg', N'6.7 inch AMOLED',  N'12GB', N'256GB', N'5000 mAh', N'Camera tele tiềm vọng');
GO

INSERT INTO NhanVien (HoTen, GioiTinh, NgaySinh, SoDienThoai, Email, DiaChi, ChucVu) VALUES
(N'Nguyễn Văn Quản', N'Nam', '1995-03-12', '0901000001', 'admin@shopdt.vn', N'Hà Nội',   N'Quản lý'),
(N'Trần Thị Lan',    N'Nữ',  '2000-07-25', '0901000002', 'lan@shopdt.vn',   N'Hà Nội',   N'Nhân viên bán hàng'),
(N'Lê Minh Tuấn',    N'Nam', '1999-11-03', '0901000003', 'tuan@shopdt.vn',  N'Bắc Ninh', N'Nhân viên bán hàng');
GO

INSERT INTO KhachHang (HoTen, SoDienThoai, Email, DiaChi) VALUES
(N'Phạm Thu Hà',   '0912000001', 'ha.pham@gmail.com', N'12 Trần Thái Tông, Cầu Giấy, Hà Nội'),
(N'Hoàng Đức Anh', '0912000002', 'ducanh@gmail.com',  N'45 Tây Sơn, Đống Đa, Hà Nội'),
(N'Vũ Thị Mai',    '0912000003', 'maivu@gmail.com',   N'8 Quang Trung, Hà Đông, Hà Nội'),
(N'Đỗ Quang Huy',  '0912000004', NULL,                N'20 Nguyễn Văn Cừ, Long Biên, Hà Nội'),
(N'Bùi Ngọc Linh', '0912000005', 'linhbui@gmail.com', N'99 Nguyễn Trãi, Thanh Xuân, Hà Nội');
GO

-- Mật khẩu mặc định của mọi tài khoản mẫu: 123456
-- Khách hàng 4 (Đỗ Quang Huy) mua tại quầy, chưa đăng ký tài khoản
INSERT INTO TaiKhoan (TenDangNhap, MatKhau, VaiTro, MaNV, MaKH) VALUES
('admin',   CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', '123456'), 2), 'Admin',     1,    NULL),
('lan',     CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', '123456'), 2), 'NhanVien',  2,    NULL),
('tuan',    CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', '123456'), 2), 'NhanVien',  3,    NULL),
('thuha',   CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', '123456'), 2), 'KhachHang', NULL, 1),
('ducanh',  CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', '123456'), 2), 'KhachHang', NULL, 2),
('maivu',   CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', '123456'), 2), 'KhachHang', NULL, 3),
('ngoclinh',CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', '123456'), 2), 'KhachHang', NULL, 5);
GO

-- Đơn 1, 3, 5: khách đặt online; đơn 2: lập tại quầy; đơn 4: khách mới đặt online, chưa có nhân viên nhận
INSERT INTO DonHang (MaKH, MaNV, NgayDat, TenNguoiNhan, SdtNguoiNhan, DiaChiGiao, PhuongThucTT, TongTien, TrangThai, GhiChu) VALUES
(1, 2,    '2026-10-15 09:30', N'Phạm Thu Hà',   '0912000001', N'12 Trần Thái Tông, Cầu Giấy, Hà Nội',  N'COD',          22980000, N'Hoàn thành', NULL),
(2, 3,    '2026-10-16 14:10', N'Hoàng Đức Anh', '0912000002', N'Mua tại cửa hàng',                     N'Tiền mặt',     27990000, N'Hoàn thành', N'Mua tại quầy'),
(3, 2,    '2026-10-18 10:45', N'Vũ Thị Mai',    '0912000003', N'8 Quang Trung, Hà Đông, Hà Nội',       N'Chuyển khoản', 9780000,  N'Đang giao',  NULL),
(1, NULL, '2026-10-19 16:20', N'Nguyễn Văn Bình','0987000001', N'5 Láng Hạ, Đống Đa, Hà Nội',          N'COD',          14480000, N'Chờ xử lý',  N'Mua tặng bố, gọi trước khi giao'),
(5, 2,    '2026-10-20 08:15', N'Bùi Ngọc Linh', '0912000005', N'99 Nguyễn Trãi, Thanh Xuân, Hà Nội',   N'COD',          8990000,  N'Đã hủy',     N'Khách đổi ý');
GO

INSERT INTO ChiTietDonHang (MaDH, MaDT, SoLuong, DonGia) VALUES
(1, 1,  1, 19990000),
(1, 9,  1, 2990000),
(2, 4,  1, 27990000),
(3, 8,  2, 4890000),
(4, 5,  1, 9490000),
(4, 14, 1, 4990000),
(5, 10, 1, 8990000);
GO

/* ============================= KIỂM TRA ============================= */

SELECT N'DanhMuc' AS Bang, COUNT(*) AS SoDong FROM DanhMuc
UNION ALL SELECT N'DienThoai',      COUNT(*) FROM DienThoai
UNION ALL SELECT N'NhanVien',       COUNT(*) FROM NhanVien
UNION ALL SELECT N'KhachHang',      COUNT(*) FROM KhachHang
UNION ALL SELECT N'TaiKhoan',       COUNT(*) FROM TaiKhoan
UNION ALL SELECT N'DonHang',        COUNT(*) FROM DonHang
UNION ALL SELECT N'ChiTietDonHang', COUNT(*) FROM ChiTietDonHang;

SELECT TenDangNhap, VaiTro, MaNV, MaKH FROM TaiKhoan ORDER BY VaiTro, MaTK;

SELECT MaDH, TenKhachHang, ISNULL(TenNhanVien, N'(chưa nhận)') AS NhanVien,
       TongTien, PhuongThucTT, TrangThai
FROM   vw_DonHang
ORDER BY MaDH;
GO

USE QLBanDienThoai;
GO

SELECT * FROM dbo.TaiKhoan;
SELECT * FROM dbo.KhachHang;