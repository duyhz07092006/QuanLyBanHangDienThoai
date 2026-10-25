# 📱 Quản Lý Bán Hàng Điện Thoại

Website quản lý và bán điện thoại xây dựng bằng **ASP.NET Core MVC (.NET 10)** và **SQL Server**, thực hiện cho đồ án môn **Lập trình Web**.

Hệ thống có 3 nhóm người dùng: **Khách hàng** (xem, đặt mua, theo dõi đơn), **Nhân viên** (xử lý đơn, lập đơn tại quầy) và **Admin** (toàn quyền quản trị).

## Mục lục

- [Công nghệ sử dụng](#công-nghệ-sử-dụng)
- [Chức năng chính](#chức-năng-chính)
- [Cấu trúc thư mục](#cấu-trúc-thư-mục)
- [Yêu cầu môi trường](#yêu-cầu-môi-trường)
- [Cài đặt và chạy ứng dụng](#cài-đặt-và-chạy-ứng-dụng)
- [Cấu hình SQL Server và tạo database](#cấu-hình-sql-server-và-tạo-database)
- [Tài khoản mẫu và hướng dẫn sử dụng](#tài-khoản-mẫu-và-hướng-dẫn-sử-dụng)
- [REST API](#rest-api)
- [Kiểm thử](#kiểm-thử)
- [Lưu ý cấu hình và bảo mật](#lưu-ý-cấu-hình-và-bảo-mật)
- [Hạn chế đã biết](#hạn-chế-đã-biết)
- [Ghi nhận hình ảnh](#ghi-nhận-hình-ảnh)

## Công nghệ sử dụng

| Thành phần | Chi tiết |
|---|---|
| Nền tảng | .NET 10 (`net10.0`), ASP.NET Core MVC + Razor Views |
| Truy cập dữ liệu | Entity Framework Core SQL Server `10.0.13` (`Microsoft.EntityFrameworkCore.SqlServer`) kết hợp ADO.NET (`Microsoft.Data.SqlClient`) viết SQL thuần ở một số chức năng |
| Cơ sở dữ liệu | Microsoft SQL Server, database `QLBanDienThoai` (script ghi chú SQL Server 2022) |
| Xác thực / phân quyền | Cookie Authentication, 3 vai trò: `Admin`, `NhanVien`, `KhachHang` (phiên đăng nhập 8 giờ) |
| Giỏ hàng | Lưu trong Session (bộ nhớ của ứng dụng) |
| Giao diện | Bootstrap 5.3.3, jQuery, jQuery Validation (+ Unobtrusive) 1.21.0, Font Awesome 6.4.0 (tải qua CDN) |

> Dự án **không dùng EF Migrations**. Database được tạo bằng script SQL trong mục [Cấu hình SQL Server và tạo database](#cấu-hình-sql-server-và-tạo-database).

## Chức năng chính

### Chưa đăng nhập
- Xem danh sách điện thoại đang bán tại trang cửa hàng `/CuaHang`.
- Đăng ký tài khoản khách hàng (`/Account/Register`), đăng nhập (`/Account/Login`).
- Gọi các API đọc dữ liệu điện thoại (xem [REST API](#rest-api)).

### Mọi tài khoản đã đăng nhập
- Danh sách điện thoại (`/DienThoai`): tìm kiếm theo tên hoặc hãng, lọc theo hãng và khoảng giá, sắp xếp (giá tăng/giảm, tên, mới nhất), phân trang 8 sản phẩm/trang.
- Xem chi tiết điện thoại.

### Khách hàng (`KhachHang`)
- Thêm vào giỏ hàng, đổi số lượng, xóa khỏi giỏ (kiểm tra theo tồn kho).
- Đặt hàng online với phương thức thanh toán **COD** hoặc **Chuyển khoản**; đơn tạo ra ở trạng thái "Chờ xử lý" và tồn kho được trừ ngay.
- Xem danh sách và chi tiết đơn của chính mình (`/DonCuaToi`).
- Hủy đơn khi đơn còn "Chờ xử lý" (tồn kho được hoàn lại).

### Nhân viên (`NhanVien`) và Admin
- **Tổng quan** (`/Home`): số điện thoại đang bán, số khách hàng, số đơn chờ xử lý, doanh thu (tổng các đơn "Hoàn thành") và 5 đơn mới nhất.
- Thêm / sửa điện thoại.
- Xem danh sách và chi tiết khách hàng.
- Xem danh sách đơn hàng, xem chi tiết và chuyển trạng thái theo luồng:
  `Chờ xử lý → Đang giao → Hoàn thành`; có thể `Đã hủy` từ "Chờ xử lý" hoặc "Đang giao" (hoàn lại tồn kho). Nếu đơn chưa có nhân viên phụ trách, hệ thống gán nhân viên đang xử lý.
- **Lập đơn tại quầy** (`/LapDon`): nhập khách theo số điện thoại (tự tạo khách mới nếu chưa có), chọn nhân viên lập đơn, chọn sản phẩm và số lượng; đơn được tạo ở trạng thái "Hoàn thành" và trừ tồn kho.

### Chỉ Admin
- Quản lý hãng điện thoại (thêm, sửa, xóa; không xóa được hãng còn điện thoại).
- Xóa điện thoại: nếu điện thoại đã nằm trong đơn hàng thì chuyển sang "Ngừng bán" thay vì xóa.
- Quản lý nhân viên: xem danh sách và thêm mới.
- Xóa khách hàng cùng tài khoản liên kết, giữ lại lịch sử đơn hàng (cần chạy thêm script, xem [bước 2 phần database](#bước-2-chạy-script-bổ-sung-cho-chức-năng-xóa-khách-hàng)).

## Cấu trúc thư mục

```text
QuanLyBanHangDienThoai/
├── Controllers/
│   ├── Api/DienThoaiApiController.cs     # REST API điện thoại
│   ├── AccountController.cs              # Đăng nhập / đăng ký / đăng xuất
│   ├── CuaHangController.cs              # Trang cửa hàng công khai
│   ├── HomeController.cs                 # Trang tổng quan (Admin, Nhân viên)
│   ├── DienThoaiController.cs            # Danh sách, thêm, sửa, xóa điện thoại
│   ├── DanhMucController.cs              # Quản lý hãng (Admin)
│   ├── GioHangController.cs              # Giỏ hàng, đặt hàng (Khách hàng)
│   ├── DonCuaToiController.cs            # Đơn của khách hàng
│   ├── DonHangController.cs              # Quản lý đơn hàng (Admin, Nhân viên)
│   ├── KhachHangController.cs            # Quản lý khách hàng
│   ├── LapDonController.cs               # Lập đơn tại quầy
│   └── NhanVienController.cs             # Quản lý nhân viên (Admin)
├── Database/
│   ├── QLBanDienThoai.sql                # Script tạo database + dữ liệu mẫu
│   ├── AllowCustomerDeletion_An_Toan.sql # Script bổ sung: cho phép xóa khách hàng
│   └── Rollback_AllowCustomerDeletion.sql# Hoàn tác script bổ sung
├── Models/                               # Entity, DbContext, model dùng chung
├── Services/                             # StoreRepository (ADO.NET), bộ lọc truy vấn, Session giỏ hàng
├── ViewModels/                           # ViewModel đặt hàng, lọc điện thoại, DTO của API
├── Views/                                # Razor Views theo từng controller + Shared layout
├── wwwroot/                              # CSS, JS, hình ảnh sản phẩm, thư viện (Bootstrap, jQuery)
├── Properties/launchSettings.json        # Cổng và profile chạy
├── Program.cs                            # Cấu hình dịch vụ và pipeline
├── appsettings.json                      # Chuỗi kết nối, logging
├── QuanLyBanHangDienThoai.csproj
└── QuanLyBanHangDienThoai.slnx
```

## Yêu cầu môi trường

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (kiểm tra bằng `dotnet --version`).
- Microsoft SQL Server (script ghi chú SQL Server 2022). Có thể dùng bản Developer hoặc Express.
- Công cụ chạy script SQL: SQL Server Management Studio (SSMS), Azure Data Studio hoặc `sqlcmd`.
- Git.
- Tùy chọn: Visual Studio bản hỗ trợ .NET 10 và định dạng solution `.slnx` để mở `QuanLyBanHangDienThoai.slnx`.

Chuỗi kết nối mặc định dùng **Windows Authentication** (`Integrated Security=True`), phù hợp khi chạy trên Windows. Nếu dùng macOS/Linux hoặc SQL Server đăng nhập bằng tài khoản SQL, xem phần [cấu hình chuỗi kết nối](#bước-3-cấu-hình-chuỗi-kết-nối).

## Cài đặt và chạy ứng dụng

**1. Tải mã nguồn**

```bash
git clone https://github.com/duyhz07092006/QuanLyBanHangDienThoai.git
cd QuanLyBanHangDienThoai
```

**2. Tạo database** theo hướng dẫn ở [phần tiếp theo](#cấu-hình-sql-server-và-tạo-database) (bắt buộc, ứng dụng không tự tạo database).

**3. Cấu hình chuỗi kết nối** trong `appsettings.json` cho đúng máy của bạn (xem [bước 3](#bước-3-cấu-hình-chuỗi-kết-nối)).

**4. Khôi phục thư viện và build**

```bash
dotnet restore
dotnet build
```

**5. Chạy ứng dụng**

```bash
dotnet run
```

Mặc định ứng dụng chạy ở **http://localhost:5042** (profile `http`). Muốn chạy HTTPS tại **https://localhost:7051**:

```bash
dotnet dev-certs https --trust     # chỉ cần chạy một lần trên máy
dotnet run --launch-profile https
```

**6. Mở trình duyệt** và vào `http://localhost:5042`. Trang chủ yêu cầu đăng nhập nên bạn sẽ được chuyển tới trang đăng nhập; trang cửa hàng công khai nằm tại `/CuaHang`. Tài khoản dùng thử xem ở [phần sau](#tài-khoản-mẫu-và-hướng-dẫn-sử-dụng).

## Cấu hình SQL Server và tạo database

### Bước 1: Chạy script tạo database

File `Database/QLBanDienThoai.sql` thực hiện tuần tự: **xóa database `QLBanDienThoai` nếu đã tồn tại**, tạo lại database (collation `Vietnamese_100_CI_AI`), tạo các bảng, index, view và nạp dữ liệu mẫu.

> ⚠️ Script sẽ **xóa toàn bộ database `QLBanDienThoai` cũ** nếu có. Hãy sao lưu trước nếu bạn đang có dữ liệu cần giữ.

Chọn một trong hai cách:

- **SSMS / Azure Data Studio:** mở file `Database/QLBanDienThoai.sql`, kết nối tới SQL Server của bạn và thực thi toàn bộ file (F5).
- **sqlcmd** (dùng `-f 65001` vì file là UTF-8, tránh lỗi tiếng Việt):

  ```bash
  # Windows Authentication
  sqlcmd -S <TEN_SERVER> -E -f 65001 -i Database/QLBanDienThoai.sql

  # SQL Authentication
  sqlcmd -S <TEN_SERVER> -U <TEN_DANG_NHAP> -P <MAT_KHAU> -f 65001 -i Database/QLBanDienThoai.sql
  ```

Script tạo các bảng: `DanhMuc`, `DienThoai`, `NhanVien`, `KhachHang`, `TaiKhoan`, `DonHang`, `ChiTietDonHang`, cùng hai view `vw_DienThoai` và `vw_DonHang`. Cuối script có các câu `SELECT` đếm số dòng từng bảng để bạn kiểm tra.

Dữ liệu mẫu sau khi chạy: 6 hãng, 15 điện thoại, 3 nhân viên, 5 khách hàng, 7 tài khoản, 5 đơn hàng.

### Bước 2: Chạy script bổ sung cho chức năng xóa khách hàng

Chạy tiếp file `Database/AllowCustomerDeletion_An_Toan.sql` (kết nối vào database `QLBanDienThoai`). Script cho phép cột `DonHang.MaKH` nhận `NULL` và đổi khóa ngoại sang `ON DELETE SET NULL`, nhờ đó Admin xóa được khách hàng mà vẫn giữ lịch sử đơn hàng. Script chạy trong một transaction và có thể chạy lại nhiều lần.

- Nếu **bỏ qua bước này**, việc xóa khách hàng đã có đơn hàng sẽ phát sinh lỗi khóa ngoại.
- Muốn hoàn tác: chạy `Database/Rollback_AllowCustomerDeletion.sql` (chỉ chạy được khi không có đơn nào đang có `MaKH = NULL`).

### Bước 3: Cấu hình chuỗi kết nối

`appsettings.json` hiện có **hai** chuỗi kết nối, trỏ tới cùng một database:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=DESKTOP-TTA9PHQ;Database=QLBanDienThoai;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=5;",
  "QLBanDienThoai":    "Server=DESKTOP-TTA9PHQ;Database=QLBanDienThoai;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=5;"
}
```

| Khóa | Dùng cho |
|---|---|
| `DefaultConnection` | Entity Framework Core: đăng nhập/đăng ký, điện thoại, hãng, giỏ hàng/đặt hàng, đơn của tôi, REST API |
| `QLBanDienThoai` | ADO.NET: trang cửa hàng, tổng quan, khách hàng, quản lý đơn hàng, lập đơn tại quầy, nhân viên |

**Bắt buộc sửa cả hai khóa** cho giống nhau, vì `DESKTOP-TTA9PHQ` là tên máy của tác giả. Ví dụ giá trị `Server` (tùy máy bạn):

| Trường hợp | Giá trị `Server` |
|---|---|
| SQL Server mặc định trên máy | `localhost` hoặc `.` |
| SQL Server Express | `.\SQLEXPRESS` |
| SQL Server qua cổng (ví dụ chạy Docker) | `localhost,1433` |

Nếu đăng nhập bằng tài khoản SQL (bắt buộc khi chạy trên macOS/Linux), thay `Integrated Security=True` bằng `User Id=<tên>;Password=<mật khẩu>`:

```text
Server=localhost,1433;Database=QLBanDienThoai;User Id=<tên>;Password=<mật khẩu>;TrustServerCertificate=True;Connect Timeout=5;
```

Để tránh đưa mật khẩu vào Git, có thể ghi đè bằng biến môi trường thay vì sửa `appsettings.json`:

```bash
# Linux / macOS
export ConnectionStrings__DefaultConnection="..."
export ConnectionStrings__QLBanDienThoai="..."
```

```powershell
# Windows PowerShell
$env:ConnectionStrings__DefaultConnection = "..."
$env:ConnectionStrings__QLBanDienThoai = "..."
```

Ứng dụng chỉ chờ kết nối 5 giây (`Connect Timeout=5`); nếu báo lỗi kết nối, hãy kiểm tra SQL Server đã chạy, tên server và quyền đăng nhập.

## Tài khoản mẫu và hướng dẫn sử dụng

Các tài khoản dưới đây do `Database/QLBanDienThoai.sql` tạo ra, **mật khẩu chung là `123456`**.

| Tên đăng nhập | Vai trò | Gắn với |
|---|---|---|
| `admin` | Admin | Nhân viên Nguyễn Văn Quản |
| `lan` | Nhân viên | Nhân viên Trần Thị Lan |
| `tuan` | Nhân viên | Nhân viên Lê Minh Tuấn |
| `thuha` | Khách hàng | Phạm Thu Hà |
| `ducanh` | Khách hàng | Hoàng Đức Anh |
| `maivu` | Khách hàng | Vũ Thị Mai |
| `ngoclinh` | Khách hàng | Bùi Ngọc Linh |

Sau khi đăng nhập, khách hàng được chuyển tới `/CuaHang`; Admin và Nhân viên được chuyển tới trang tổng quan.

**Khách hàng mua hàng**
1. Đăng nhập (hoặc đăng ký tại `/Account/Register`).
2. Tại `/CuaHang` hoặc `/DienThoai`, bấm thêm điện thoại vào giỏ.
3. Vào `/GioHang`, chỉnh số lượng rồi chọn **Đặt hàng**, điền thông tin nhận hàng và phương thức thanh toán.
4. Theo dõi tại `/DonCuaToi`; đơn còn "Chờ xử lý" có thể tự hủy.

**Nhân viên / Admin xử lý đơn**
1. Đăng nhập bằng `lan` hoặc `admin`, vào **Đơn hàng** (`/DonHang`) và mở một đơn.
2. Dùng các nút chuyển trạng thái trong trang chi tiết theo đúng luồng `Chờ xử lý → Đang giao → Hoàn thành` (hoặc hủy).
3. Bán trực tiếp: vào **Lập đơn** (`/LapDon`), nhập số điện thoại và họ tên khách, chọn nhân viên lập đơn, nhập số lượng ở các sản phẩm cần bán rồi xác nhận.

**Admin quản trị**
- Hãng: `/DanhMuc`; Nhân viên: `/NhanVien`; Khách hàng: `/KhachHang`; Điện thoại: `/DienThoai`.

> Số điện thoại lưu tối đa **11 ký tự** (theo cột `VARCHAR(11)` trong database), nhập dài hơn sẽ gây lỗi khi lưu.

## REST API

Đường dẫn gốc `/api/dienthoai` (ví dụ `http://localhost:5042/api/dienthoai`).

| Phương thức | Đường dẫn | Quyền |
|---|---|---|
| `GET` | `/api/dienthoai` | Công khai |
| `GET` | `/api/dienthoai/{id}` | Công khai |
| `POST` | `/api/dienthoai` | `Admin`, `NhanVien` |
| `PUT` | `/api/dienthoai/{id}` | `Admin`, `NhanVien` |
| `DELETE` | `/api/dienthoai/{id}` | `Admin` |

Tham số của `GET /api/dienthoai`: `tuKhoa`, `maDM`, `giaTu`, `giaDen`, `sapXep` (`gia_tang`, `gia_giam`, `ten`, `moi`), `trang` (mặc định 1), `kichThuoc` (mặc định 10, tối đa 50). Kết quả là JSON phân trang gồm `duLieu`, `trangHienTai`, `kichThuocTrang`, `tongSoDong`, `tongSoTrang`.

```bash
curl "http://localhost:5042/api/dienthoai?tuKhoa=iphone&sapXep=gia_tang&trang=1&kichThuoc=5"
curl "http://localhost:5042/api/dienthoai/1"
```

API dùng chung cookie đăng nhập với website. Khi chưa đăng nhập hoặc không đủ quyền, các đường dẫn `/api/...` trả về `401` hoặc `403` (không chuyển hướng sang trang đăng nhập).

## Kiểm thử

Dự án **chưa có test tự động** (không có project test, `.csproj` không có gói test). Thư mục `.postman` và `postman/` chỉ chứa cấu hình workspace Postman, chưa có collection.

Có thể kiểm tra thủ công trên database vừa tạo từ script (dữ liệu mẫu ban đầu):

| # | Thao tác | Kết quả mong đợi |
|---|---|---|
| 1 | Đăng nhập `admin` / `123456` | Vào trang tổng quan: 15 điện thoại, 5 khách hàng, 1 đơn chờ xử lý, doanh thu 50.970.000 đ |
| 2 | Đăng nhập `ducanh` / `123456` | Chuyển tới `/CuaHang`; `/DonCuaToi` có 2 đơn (#1 và #4) |
| 3 | Với `ducanh`, mở đơn #17 ("Chờ xử lý") và bấm hủy | Đơn chuyển "Đã hủy", tồn kho iPhone 15 Pro Max 256GB tăng thêm 1 |
| 4 | Với `ducanh`, thêm một điện thoại vào giỏ và đặt hàng | Tạo đơn "Chờ xử lý", tồn kho giảm đúng số lượng đã đặt |
| 5 | Đăng nhập `lan`, mở đơn mới và chuyển trạng thái | Chuyển được theo luồng `Chờ xử lý → Đang giao → Hoàn thành`; không chuyển ngược được |
| 6 | Với tài khoản khách hàng, truy cập `/DanhMuc` | Hiện trang từ chối truy cập (`/Account/AccessDenied`) |
| 7 | Chưa đăng nhập, truy cập `/DienThoai` | Chuyển tới trang đăng nhập |
| 8 | Chưa đăng nhập, gửi `POST /api/dienthoai` | Trả về `401` |

Sau khi thử, chạy lại `Database/QLBanDienThoai.sql` (và script bổ sung ở bước 2) để đưa dữ liệu về trạng thái ban đầu.

## Lưu ý cấu hình và bảo mật

- **Chuỗi kết nối:** `appsettings.json` đang chứa tên máy của tác giả và dùng Windows Authentication; mỗi người cần sửa lại cho máy mình. Không commit mật khẩu SQL lên GitHub, hãy dùng biến môi trường như hướng dẫn ở trên.
- **Mật khẩu:** được băm bằng SHA-256 (chuỗi hex), **chưa có salt**. Cách này chỉ phù hợp cho đồ án; hệ thống thật nên dùng cơ chế băm chuyên dụng (ví dụ ASP.NET Core Identity).
- **Tài khoản mẫu** dùng chung mật khẩu `123456`, chỉ để thử nghiệm. Đổi hoặc xóa trước khi triển khai thật.
- **Script `QLBanDienThoai.sql` xóa database cũ.** Đừng chạy trên database đang có dữ liệu thật.
- **Giỏ hàng** lưu trong bộ nhớ ứng dụng, sẽ mất khi ứng dụng khởi động lại.
- Các form `POST` đều dùng anti-forgery token, truy vấn SQL dùng tham số; cookie đăng nhập hết hạn sau 8 giờ (tự gia hạn khi còn hoạt động).
- Giá trị `AllowedHosts` đang là `*`; nên giới hạn lại khi triển khai thật.
- Khi chạy HTTPS lần đầu, cần tin cậy chứng chỉ phát triển bằng `dotnet dev-certs https --trust`.

## Hạn chế đã biết

Các điểm dưới đây được ghi nhận từ mã nguồn hiện tại:

- Đường dẫn `/LapDon` **chưa gắn `[Authorize]`**: menu chỉ hiện cho Admin/Nhân viên nhưng người biết URL vẫn truy cập được.
- Quản lý nhân viên: chức năng **Sửa** chưa có view `Edit.cshtml`, nút **Xóa** là liên kết `GET` trong khi action chỉ nhận `POST`, nên hiện chỉ xem danh sách và thêm mới hoạt động đầy đủ.
- Giao diện chưa có chức năng tạo tài khoản đăng nhập cho nhân viên; tài khoản `admin`, `lan`, `tuan` đến từ script SQL.

## Ghi nhận hình ảnh

Ảnh sản phẩm nằm trong `wwwroot/images`. Nguồn và giấy phép của các ảnh được ghi trong `wwwroot/images/ATTRIBUTION.txt`.
