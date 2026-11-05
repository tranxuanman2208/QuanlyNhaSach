# HƯỚNG DẪN NẠP DỮ LIỆU SÁCH THẬT VÀO SQL SERVER (`README_BOOK_DATA_ENTRY.md`)

Tài liệu này hướng dẫn chi tiết quy trình đăng nhập, chuẩn bị khóa ngoại `MaNCC`, thêm/sửa dữ liệu sách thật trên SQL Server thông qua giao diện WPF của dự án **QuanlyNhaSach**, cũng như cách hệ thống xử lý các trường mở rộng (`MoTa`, `AnhBia`, `MauBia`).

---

## 1. Cấu hình Kết nối Cơ sở dữ liệu (SQL Server)

- **DbContext**: `QuanlyNhaSach.Models.QuanlyNhaSachContext`
- **Chuỗi kết nối hiện tại** (trong `QuanlyNhaSach/Models/QuanLyNhaSachContext.cs`):
  ```
  Data Source=.\SQLEXPRESS;Initial Catalog=QuanlyNhaSach;Integrated Security=True;Trust Server Certificate=True
  ```
- **Điều kiện tiên quyết trước khi nhập sách**:
  1. Dịch vụ **SQL Server (`MSSQL$SQLEXPRESS`)** phải đang chạy (`Running`).
  2. Cơ sở dữ liệu **`QuanlyNhaSach`** đã được khởi tạo trên `.\SQLEXPRESS` bằng script `QuanlyNhaSach/DBQuanLyNhaSach.sql`.
  3. Bảng **`NHACUNGCAP`** phải có ít nhất 01 bản ghi nhà cung cấp hợp lệ (do cột `MaNCC` của bảng `SACH` tham chiếu khóa ngoại `fk_MaNCC` tới `NHACUNGCAP(MaNCC)`).

---

## 2. Tài khoản Đăng nhập & Phân quyền Kiểm thử (STEP 4.1)

Hệ thống xác thực (`Services/AuthService.cs` & `Views/Auth/AuthWindow.xaml`) hiện cung cấp các tài khoản thử nghiệm tĩnh trên bộ nhớ (In-Memory Demo Accounts) và chế độ Khách vãng lai (Guest) để phục vụ kiểm thử và nạp dữ liệu sách:

### 2.1. Quản lý (Manager Portal — Dùng để Thêm / Sửa / Xóa Sách)
- **Username**: `manager@example.com`
- **Password**: `Manager123!`
- **Role**: `Manager` (`UserRole.Manager`)
- **Màn hình đích**: `ManagerMainWindow` (Cổng Quản Trị)
- **Mẹo thao tác nhanh**: Tại màn hình Đăng nhập (`AuthWindow`), bạn có thể nhấn nút màu cam **"TK Quản Lý (`manager@example.com`)"** ở góc dưới bên trái để tự động điền thông tin, sau đó nhấn **"Đăng nhập hệ thống"**.

### 2.2. Khách hàng / Khách vãng lai (Customer / Guest Flow)
- **Cách 1 — Tiếp tục với tư cách Khách (Không cần tài khoản)**:
  - Nhấn nút **"Tiếp tục với tư cách Khách"** ngay bên dưới nút *"Đăng nhập hệ thống"* tại màn hình `AuthWindow`.
  - Hệ thống tạo phiên làm việc `Khách vãng lai (Guest)` với phân quyền phạm vi `Customer` (không có quyền truy cập `Staff` hay `Manager`).
- **Cách 2 — Tài khoản Khách hàng mẫu**:
  - **Username**: `customer@example.com`
  - **Password**: `Customer123!`
- **Màn hình đích**: `CustomerMainWindow` (`HomePage`)
- **Các quyền hiện được hỗ trợ**:
  - Xem toàn bộ danh mục sách thật từ SQL Server thông qua `BookService`.
  - Tìm kiếm sách theo Tên sách, Tác giả, Mã sách; lọc theo tình trạng tồn kho (`Còn hàng`, `Hết hàng`) và sắp xếp theo Giá bán.
  - Xem thông tin chi tiết sách (`BookDetailPage`) và thêm sách vào giỏ hàng tạm thời.
  - *Lưu ý*: Chức năng xác nhận Đặt hàng (`OrderService`) hiện đang đóng băng chờ tích hợp DB ở giai đoạn tiếp theo.

### 2.3. Nhân viên (Staff Portal — Tra cứu Sách & Tồn kho)
- **Username**: `staff@example.com`
- **Password**: `Staff123!`
- **Role**: `Staff` (`UserRole.Staff`)
- **Màn hình đích**: `StaffMainWindow` (Cổng Nhân Viên)
- **Chức năng hoạt động với dữ liệu thật**:
  - Chọn menu **"Tra cứu"** (`StaffLookupPage`) ở thanh bên trái để tra cứu danh mục sách và số lượng tồn kho thực tế từ SQL Server thông qua `BookService.SearchAndFilterBooks(...)`.
  - *Lưu ý*: Các nghiệp vụ Xử lý đơn hàng, Bán hàng/Thu tiền và Xuất kho đang tạm đóng băng.

---

## 3. Đường dẫn Thao tác Thêm & Sửa Sách (STEP 4.2)

Luồng giao diện thực tế để nạp sách vào SQL Server:

```text
Chạy ứng dụng (dotnet run hoặc từ Visual Studio)
   ↓
Màn hình Đăng nhập (AuthWindow)
   ↓ Nhấn "TK Quản Lý" hoặc nhập manager@example.com / Manager123! → Nhấn "Đăng nhập hệ thống"
Cổng Quản Trị (ManagerMainWindow)
   ↓ Nhấn mục số 2 "Quản lý sách" trên thanh menu dọc bên trái (Sidebar)
Màn hình Quản lý sách (BookManagementPage)
   ↓ Nhấn nút màu cam "+ Thêm sách mới" ở góc trên bên phải
Hộp thoại "Thêm đầu sách mới (UC09)"
   ↓ Nhập đầy đủ các trường bắt buộc (*) → Nhấn nút "Lưu thông tin sách"
BookService.AddBook() kiểm tra ràng buộc & gọi db.SaveChanges()
   ↓
SQL Server (Bảng dbo.SACH) lưu bản ghi & DataGrid tự động làm mới (RefreshBooks)
```

### Cách chỉnh sửa sách đã có:
1. Tại bảng danh sách sách (`DataGrid`) trên màn hình **Quản lý sách**, tìm dòng sách cần sửa.
2. Nhấn nút **"Sửa"** ở cột **Thao tác** (cột cuối cùng bên phải).
3. Hộp thoại **"Chỉnh sửa thông tin sách #[Mã sách]"** hiện ra (trường `Mã sách` ở chế độ chỉ đọc `IsReadOnly` vì là khóa chính).
4. Thay đổi Tên sách, Tác giả, Thể loại, NXB, Mã nhà cung cấp (`MaNCC`), Giá bán hoặc Số lượng tồn kho.
5. Nhấn **"Lưu thông tin sách"** để cập nhật xuống SQL Server.

---

## 4. Cảnh báo Quan trọng về Khóa ngoại `MaNCC` (STEP 4.3)

### 4.1. Bản chất ràng buộc trong SQL Server
Trong schema SQL Server (`DBQuanLyNhaSach.sql` và `QuanLyNhaSachContext.cs`), cột `MaNCC` của bảng `dbo.SACH` có ràng buộc khóa ngoại **`fk_MaNCC`** tham chiếu trực tiếp tới cột `MaNCC` của bảng **`dbo.NHACUNGCAP`**:

```sql
ALTER TABLE [dbo].[SACH] WITH CHECK ADD CONSTRAINT [fk_MaNCC] FOREIGN KEY([MaNCC])
REFERENCES [dbo].[NHACUNGCAP] ([MaNCC])
```

- **Người dùng tuyệt đối không được nhập tùy ý một mã `MaNCC` chưa tồn tại trong bảng `NHACUNGCAP`.**
- Nếu nhập một mã không tồn tại (ví dụ `NCC99` khi bảng `NHACUNGCAP` chưa có `NCC99`), `BookService` sẽ chặn ngay trước khi lưu và hiển thị danh sách các mã `MaNCC` hợp lệ hiện có trong Database (hoặc cảnh báo nếu bảng `NHACUNGCAP` đang trống).

### 4.2. Câu lệnh SQL kiểm tra Mã nhà cung cấp hiện có
Trước khi thêm sách, bạn có thể mở **SQL Server Management Studio (SSMS)** hoặc dùng `sqlcmd` chạy câu truy vấn chỉ đọc sau trên database `QuanlyNhaSach`:

```sql
-- Kiểm tra danh sách mã nhà cung cấp
SELECT MaNCC FROM NHACUNGCAP;

-- Hoặc kiểm tra cả mã và tên nhà cung cấp
SELECT MaNCC, TenNCC FROM NHACUNGCAP;
```

> **Gợi ý trên giao diện**: Khi mở hộp thoại **Thêm đầu sách mới** hoặc **Chỉnh sửa thông tin sách**, hệ thống cũng tự động đọc danh sách `MaNCC` từ bảng `NHACUNGCAP` và hiển thị gợi ý tại dòng chú thích của ô **Mã nhà cung cấp (`MaNCC`)**, đồng thời tự động điền sẵn mã `MaNCC` đầu tiên hợp lệ nếu bảng `NHACUNGCAP` đã có dữ liệu.

### 4.3. Cách xử lý nếu bảng `NHACUNGCAP` đang trống
Vì phân hệ Quản lý Nhà cung cấp trên giao diện đang tạm đóng băng theo kế hoạch chuyển đổi từng giai đoạn, nếu câu lệnh `SELECT MaNCC, TenNCC FROM NHACUNGCAP;` trả về 0 dòng, bạn cần thêm ít nhất một nhà cung cấp trực tiếp trong SQL Server trước khi thêm sách trên giao diện:

```sql
USE [QuanlyNhaSach];
GO

INSERT INTO dbo.NHACUNGCAP (MaNCC, TenNCC, Email, SDT, Diachi)
VALUES
    ('NCC01', N'NXB Trẻ', 'contact@nxbtre.com.vn', '0283931628', N'161B Lý Chính Thắng, Q.3, TP.HCM'),
    ('NCC02', N'Nhã Nam', 'info@nhanam.vn', '0283846270', N'59 Đỗ Quang, Cầu Giấy, Hà Nội');
GO
```

Sau khi bảng `NHACUNGCAP` đã có `NCC01`, `NCC02`, bạn có thể nhập `NCC01` hoặc `NCC02` vào ô **Mã nhà cung cấp (`MaNCC`)** khi thêm sách.

---

## 5. Cơ chế xử lý Mô tả (`MoTa`) và Ảnh bìa (`AnhBia` / `MauBia`) (STEP 4.4)

Bảng `dbo.SACH` trong SQL Server hiện chỉ gồm 9 cột vật lý:
`MaSach`, `Tensach`, `Tacgia`, `NXB`, `Giaban`, `Soluongton`, `Theloai`, `MaNCC`, `Daxoa`.

**Bảng `dbo.SACH` chưa có cột `MoTa` và chưa có cột `AnhBia` / `MauBia`.** Do đó, hệ thống xử lý hai thông tin này thông qua lớp mở rộng `Models/SACH.Extended.cs` như sau:

### 5.1. Mô tả sách (`MoTa`)
- **Khai báo**: Thuộc tính `[NotMapped] public string MoTa` trong `Models/SACH.Extended.cs`.
- **Lưu trữ**: **KHÔNG lưu vào SQL Server** (chỉ tồn tại tạm thời trên bộ nhớ RAM trong phiên làm việc hoặc sinh tự động).
- **Cơ chế Fallback an toàn**: Khi dữ liệu sách được tải từ SQL Server lên (hoặc sau khi đóng và mở lại ứng dụng), `SACH.MoTa` không bao giờ trả về `null` mà tự động tổng hợp chuỗi giới thiệu từ dữ liệu thật của sách:
  > `"<Tensach>" — Tác giả: <Tacgia> | Thể loại: <Theloai> | NXB: <NXB>.`
- **Hiển thị**: Trang Chi tiết sách (`BookDetailPage.xaml`) binding tới `{Binding SelectedBook.MoTa}` luôn hiển thị đầy đủ thông tin mà không xảy ra lỗi binding hoặc khoảng trống.

### 5.2. Ảnh bìa & Màu bìa (`AnhBia` / `MauBia`)
- **Phân biệt hai thuộc tính trong `Models/SACH.Extended.cs`**:
  - `MauBia` (`[NotMapped] public string MauBia`): Mã màu Hex dùng làm nền bìa sách cách điệu trên giao diện. Mặc định trả về `SACH.DefaultCoverColor` (`"#1E3A8A"` — Xanh đậm thương hiệu).
  - `AnhBia` (`[NotMapped] public string AnhBia`): Chuỗi định danh placeholder ảnh bìa. Mặc định trả về `SACH.DefaultCoverPlaceholder` (`"BookCoverPlaceholder"`).
- **Lưu trữ**: **KHÔNG lưu vào SQL Server** vì bảng `dbo.SACH` chưa có cột tương ứng.
- **Cơ chế Fallback trên XAML**:
  - Toàn bộ giao diện (`BookManagementPage.xaml`, `HomePage.xaml` / `Templates.xaml`, `BookDetailPage.xaml`, `CartCheckoutPage.xaml`) sử dụng `Border` kết hợp `StringToBrushConverter` để chuyển `MauBia` thành `SolidColorBrush` và hiển thị biểu tượng sách вектор (`IconBookGeometry`), Mã sách (`MaSach`), Tên sách (`Tensach`) và Tác giả (`Tacgia`).
  - Trong `Converters/ValueConverters.cs`, `StringToBrushConverter` có khối `try / catch` an toàn: nếu `MauBia` là `null`, rỗng hoặc mã màu không hợp lệ, converter tự động fallback về màu `#1E3A8A`, đảm bảo giao diện không bao giờ văng ứng dụng (`UnhandledException`).

---

## 6. Bảng Quy tắc Nhập liệu các Trường Sách (STEP 4.5)

| Trường trên UI | Thuộc tính Entity `SACH` | Bắt buộc? | Kiểu C# / SQL Server | Quy tắc & Lưu ý khi nhập liệu |
| :--- | :--- | :---: | :--- | :--- |
| **Mã sách \*** | `MaSach` | **Bắt buộc** | `string` / `varchar(10) NOT NULL` | Khóa chính (`PK`). Tối đa **10 ký tự** không dấu, không trùng với mã sách đã có trong bảng `SACH` (ví dụ: `S001`, `SACH01`). Không thể sửa sau khi tạo. |
| **Tên sách \*** | `Tensach` | **Bắt buộc** (theo Validation) | `string?` / `nvarchar(100)` | Tối đa **100 ký tự**, hỗ trợ tiếng Việt có dấu. Cặp (`Tensach`, `Tacgia`) không được trùng với sách đang hoạt động. |
| **Tác giả \*** | `Tacgia` | **Bắt buộc** (theo Validation) | `string?` / `nvarchar(100)` | Tối đa **100 ký tự**, hỗ trợ tiếng Việt có dấu. |
| **Thể loại** | `Theloai` | Tùy chọn (Mặc định: `"Văn học & Kỹ năng"`) | `string?` / `nvarchar(100)` | Tối đa **100 ký tự**. |
| **Nhà xuất bản (NXB) \*** | `NXB` | **Bắt buộc** (theo Validation) | `string?` / `nvarchar(100)` | Tối đa **100 ký tự** (ví dụ: `NXB Trẻ`, `NXB Kim Đồng`). |
| **Mã nhà cung cấp (`MaNCC`) \*** | `MaNCC` | **Bắt buộc** (theo Validation & FK) | `string?` / `varchar(10)` | Tối đa **10 ký tự**. **Bắt buộc phải tồn tại trước trong bảng `dbo.NHACUNGCAP`** (ràng buộc `fk_MaNCC`). |
| **Giá bán (VND > 0) \*** | `Giaban` | **Bắt buộc** (theo Validation) | `decimal?` / `decimal(10, 2)` | Phải là số dương `> 0` và `<= 99,999,999.99` (ví dụ: `120000`). Có thể nhập kèm dấu chấm/phẩy phân cách nghìn, hệ thống tự làm sạch. |
| **Số lượng tồn kho (0 - 255) \*** | `Soluongton` | **Bắt buộc** (theo Validation) | `byte?` / `tinyint` | Số nguyên từ **`0` đến `255`** (giới hạn kiểu `tinyint` của SQL Server). Nếu `< 10` và `> 0` sẽ hiển thị cảnh báo *"Sắp hết"*; nếu `= 0` hiển thị *"Hết hàng"*. |
| **Mô tả** | `MoTa` (`SACH.Extended`) | Không nhập trên form | `string` (`[NotMapped]`) | Không có cột trong DB. Tự động sinh chuỗi giới thiệu fallback từ Tên sách, Tác giả, Thể loại, NXB. |
| **Ảnh bìa / Màu bìa** | `AnhBia` / `MauBia` (`SACH.Extended`) | Không nhập trên form | `string` (`[NotMapped]`) | Không có cột trong DB. Tự động hiển thị bìa sách mặc định màu `#1E3A8A` cùng mã sách và tiêu đề. |

---

## 7. Hướng dẫn Xử lý các Lỗi Thường gặp (STEP 4.6)

1. **Lỗi Khóa ngoại `MaNCC` (`fk_MaNCC` / Foreign Key Constraint)**:
   - *Biểu hiện*: Hộp thoại báo `"Mã nhà cung cấp '...' không tồn tại trong bảng NHACUNGCAP (ràng buộc khóa ngoại fk_MaNCC)..."`.
   - *Cách xử lý*: Chạy `SELECT MaNCC, TenNCC FROM NHACUNGCAP;` trong SSMS để xem các mã hợp lệ, hoặc chạy lệnh `INSERT INTO dbo.NHACUNGCAP ...` (mục 4.3 ở trên) để tạo mã nhà cung cấp trước khi thêm sách.
2. **Lỗi Trùng Khóa chính (`Duplicate Primary Key`) hoặc Trùng sách**:
   - *Biểu hiện*: Hộp thoại báo `"Mã sách '...' đã bị trùng trong hệ thống!"` hoặc `"Sách có cùng tên và tác giả này đã tồn tại trong hệ thống!"`.
   - *Cách xử lý*: Đặt mã `MaSach` khác (chưa từng tồn tại trong bảng `dbo.SACH`, kể cả các sách đã xóa mềm `Daxoa = 1`).
3. **Lỗi Số lượng tồn kho vượt quá 255 (`byte` / `tinyint`)**:
   - *Biểu hiện*: Hộp thoại báo `"Số lượng tồn kho không hợp lệ. Vui lòng nhập số nguyên từ 0 đến 255."`.
   - *Nguyên nhân*: Cột `Soluongton` trong bảng `dbo.SACH` có kiểu `tinyint` (0..255).
   - *Cách xử lý*: Nhập giá trị nguyên trong khoảng `0` đến `255`.
4. **Lỗi Giá bán vượt quá giới hạn `decimal(10, 2)`**:
   - *Biểu hiện*: Hộp thoại báo `"Giá bán không hợp lệ. Vui lòng nhập số dương từ 1 đến 99,999,999..."`.
   - *Cách xử lý*: Nhập giá bán lớn hơn `0` và nhỏ hơn `100,000,000`.
5. **Lỗi Kết nối SQL Server (`Cannot open database "QuanlyNhaSach"` / `SqlException`)**:
   - *Biểu hiện*: Khi nhấn Lưu sách báo `"Lỗi kết nối hoặc truy vấn SQL Server: Cannot open database 'QuanlyNhaSach' requested by the login..."`, hoặc danh sách sách trống khi mở màn hình.
   - *Nguyên nhân*: Instance `.\SQLEXPRESS` chưa có database `QuanlyNhaSach` hoặc dịch vụ SQL Server đang tắt.
   - *Cách xử lý*:
     1. Mở SSMS, kết nối vào `.\SQLEXPRESS` bằng Windows Authentication.
     2. Tạo database `QuanlyNhaSach` (`CREATE DATABASE [QuanlyNhaSach];`).
     3. Mở file `QuanlyNhaSach/DBQuanLyNhaSach.sql` và nhấn **Execute (F5)** để tạo đầy đủ các bảng và khóa ngoại.
     4. Thêm ít nhất 1 bản ghi vào bảng `NHACUNGCAP`, sau đó quay lại ứng dụng WPF và nhấn **"Đặt lại bộ lọc"** để tải lại dữ liệu.

---

## 8. Kiểm tra Dữ liệu Đã Lưu trong SQL Server

Sau khi thêm hoặc sửa sách thành công trên giao diện WPF, bạn có thể xác nhận dữ liệu thực tế trong SQL Server bằng câu lệnh:

```sql
USE [QuanlyNhaSach];
GO

SELECT MaSach, Tensach, Tacgia, NXB, Giaban, Soluongton, Theloai, MaNCC, Daxoa
FROM dbo.SACH
ORDER BY MaSach;
GO
```
