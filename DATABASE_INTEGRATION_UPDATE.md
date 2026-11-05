# TỔNG HỢP CẬP NHẬT TÍCH HỢP CƠ SỞ DỮ LIỆU & LOẠI BỎ MOCK DATA
**Dự án:** `QuanlyNhaSach — Bookstore Management System`  
**Nền tảng:** C# .NET 8 WPF (MVVM Toolkit, Entity Framework Core 8, Microsoft SQL Server)  
**Thời gian hoàn thành:** 05/11/2026  

---

## 1. TỔNG QUAN CHIẾN DỊCH REFACTORING

Hệ thống đã hoàn tất chiến dịch di chuyển toàn diện từ kiến trúc dữ liệu mẫu (Mock Data / In-Memory / Temporary Compatibility Models) sang cơ sở dữ liệu thật Microsoft SQL Server thông qua Entity Framework Core 8 (`QuanlyNhaSachContext`):

- **Xóa bỏ 100% Mock Models:** Loại bỏ hoàn toàn `TemporaryCompatibilityModels.cs` (`Order`, `OrderItem`, `Supplier`, `ImportReceipt`, `ImportReceiptDetail`) và các model người dùng cũ (`User.cs`, `Customer.cs`, `Staff.cs`, `Manager.cs`).
- **Khôi phục toàn bộ Real EF Core Services:** Tái cấu trúc hoàn chỉnh 5 dịch vụ nghiệp vụ chính kết nối trực tiếp CSDL: `SupplierService`, `ImportService`, `OrderService`, `InventoryService`, `UserManagementService` và dịch vụ xác thực `AuthService`.
- **Đồng bộ tầng hiển thị (Presentation Layer):** Chuyển đổi toàn bộ ViewModels, Commands, XAML Bindings, Converters và Navigation Service sang Entity thực.
- **Bảo toàn tính toàn vẹn dữ liệu (Data Integrity):** Áp dụng EF Core Atomic Transactions (`BeginTransaction`), kiểm soát số lượng tồn kho (`byte?` 0..255), khóa ngoại và chống xuất kho lặp lại.
- **Bảo vệ tuyệt đối Protected Files:** Không can thiệp bất kỳ dòng code nào vào hai file `BookService.cs` và `BookManagementViewModel.cs` (SHA-256 hash giữ nguyên 100%).
- **Kết quả Build:** Đạt chuẩn `Build succeeded — 0 Errors`.

---

## 2. CHI TIẾT CÁC CẬP NHẬT THEO PHÂN HỆ

### 2.1. Kiến trúc Models & Partial Extensions
Tận dụng cơ chế `partial class` của C# để bổ sung các thuộc tính tính toán và dữ liệu phục vụ UI binding (`[NotMapped]`) mà không cần thay đổi các file scaffold gốc từ EF Core:

- **[`NHACUNGCAP.Extended.cs`](QuanlyNhaSach/Models/NHACUNGCAP.Extended.cs):**
  - Bổ sung các property UI: `SupplierCode`, `DiaChi`, `IsActive`, `StatusLabel`, `StatusText`, `ToggleStatusActionText`, `DisplayName`.
  - Hỗ trợ lưu trữ trạng thái hoạt động/ngừng hợp tác qua tiền tố/hậu tố an toàn mà không phá vỡ schema SQL.
- **[`PHIEUNHAP.Extended.cs`](QuanlyNhaSach/Models/PHIEUNHAP.Extended.cs):**
  - Mở rộng `PHIEUNHAP`: `ReceiptCode`, `TenNCC`, `SdtNCC`, `EmailNCC`, `DiaChiNCC`, `NgayLap`, `TongTien`, `TrangThai`, `NguoiLapHoTen`, `GhiChu`, `Details`, `DistinctBookCount`, `TotalImportedQuantity`, `BooksSummary`, `FormattedTongTien`, `FormattedNgayLap`, `TrangThaiLabel`.
  - Mở rộng `CHITIETPHIEUNHAP` (kế thừa `ObservableObject`): `BookCode`, `TenSach`, `TacGia`, `CurrentStock`, `SoLuongNhap`, `DonGiaNhap`, `ThanhTien`, `FormattedDonGiaNhap`, `FormattedThanhTien`, thông báo thay đổi số lượng/giá tức thì trên UI.
- **[`DONHANG.Extended.cs`](QuanlyNhaSach/Models/DONHANG.Extended.cs):**
  - Mở rộng `DONHANG` (kế thừa `ObservableObject`): `OrderCode`, `HoTenKhachHang`, `SDTKhachHang`, `DiaChiGiaoHang`, `CustomerSummary`, `DisplayTitle`, `NgayDat`, `NgayCapNhat`, `StockOutDate`, `TongTien`, `PaidAmount`, `PaymentNote`, `Status`, `Method`, `PaymentStatus`, `StockOutStatus`, `Items`, `TotalQuantity`, `BooksSummary`, `FormattedTongTien`, `FormattedPaidAmount`, `FormattedNgayDat`, `CanRecordPayment`, `CanTransitionStatus`, `RequiredActionText`.
  - Mở rộng `CHITIETDONHANG`: `TenSach`, `TacGia`, `SoLuongMua`, `DonGia`, `ThanhTien`, `FormattedDonGia`, `FormattedThanhTien`.
- **[`NGUOIDUNG.Extended.cs`](QuanlyNhaSach/Models/NGUOIDUNG.Extended.cs):**
  - Mở rộng `NGUOIDUNG`: `UserCode`, `HoTen`, `DiaChi`, `Username`, `Password`, `Role`, `IsActive`, `RoleDisplayName`, `AccountStatusLabel`, `ToggleLockActionText`, `Initials`.
- **[`StockOutLineItem.cs`](QuanlyNhaSach/Models/StockOutLineItem.cs):**
  - Cập nhật adapter nhận `CHITIETDONHANG` và `SACH` thực tế từ EF Core.
- **[`ManagerOverviewModels.cs`](QuanlyNhaSach/Models/ManagerOverviewModels.cs):**
  - Chuyển đổi thuộc tính `MaSach` sang kiểu `string` tương thích với `SACH.MaSach` (`varchar(10)`).

---

### 2.2. Phân hệ Nhà cung cấp (Supplier Module)
- **Service ([`SupplierService.cs`](QuanlyNhaSach/Services/SupplierService.cs)):**
  - Viết lại toàn bộ với EF Core và `db.NHACUNGCAPs`.
  - Truy vấn tối ưu hóa bằng `.AsNoTracking()`.
  - Tự động sinh mã `MaNCC` chuẩn định dạng (`NCC01`, `NCC02`, ...).
  - Kiểm tra tính hợp lệ: SĐT đúng 10 số, kiểm tra trùng tên/SĐT/email.
  - Kiểm tra ràng buộc khóa ngoại trước khi xóa/ngừng hợp tác: nếu NCC đã có sách (`SACH`) hoặc phiếu nhập (`PHIEUNHAP`), chuyển sang trạng thái ngừng hợp tác thay vì xóa cứng gây lỗi DB.
- **Presentation Layer:**
  - Cập nhật [`SupplierManagementViewModel.cs`](QuanlyNhaSach/ViewModels/Manager/SupplierManagementViewModel.cs) dùng `NHACUNGCAP`.
  - Cập nhật [`SupplierManagementPage.xaml`](QuanlyNhaSach/Views/Manager/SupplierManagementPage.xaml) giới hạn nhập SĐT 10 ký tự.

---

### 2.3. Phân hệ Nhập kho (Import Module)
- **Service ([`ImportService.cs`](QuanlyNhaSach/Services/ImportService.cs)):**
  - Viết lại toàn bộ sử dụng `db.PHIEUNHAPs`, `db.CHITIETPHIEUNHAPs`, `db.NHACUNGCAPs`, `db.SACHes`, `db.NGUOIDUNGs`.
  - Lấy lịch sử phiếu nhập kèm Eager Loading (`Include(r => r.MaNCCNavigation)`, `Include(r => r.MaQlyNavigation)`, `Include(r => r.CHITIETPHIEUNHAPs).ThenInclude(...)`).
  - Giao dịch nguyên tử (`BeginTransaction`) khi xác nhận phiếu nhập:
    - Lưu thông tin `PHIEUNHAP`.
    - Lưu từng chi tiết `CHITIETPHIEUNHAP`.
    - Tự động cộng tồn kho `SACH.Soluongton` an toàn (kiểm tra cận trên 255 của `tinyint` và giới hạn `decimal(10,2)`).
    - Tự động Rollback nếu xảy ra bất kỳ lỗi dữ liệu nào.
- **Presentation Layer:**
  - Viết lại [`ImportReceiptViewModel.cs`](QuanlyNhaSach/ViewModels/Manager/ImportReceiptViewModel.cs) và [`ImportHistoryViewModel.cs`](QuanlyNhaSach/ViewModels/Manager/ImportHistoryViewModel.cs).
  - Cập nhật [`ImportReceiptPage.xaml`](QuanlyNhaSach/Views/Manager/ImportReceiptPage.xaml) khớp các binding thuộc tính `SACH` (`MaSach`, `Tensach`, `Tacgia`).

---

### 2.4. Phân hệ Đơn hàng & Xuất kho (Order & Inventory Modules)
- **Service Xuất kho ([`InventoryService.cs`](QuanlyNhaSach/Services/InventoryService.cs)):**
  - Kiểm tra đối chiếu tồn kho theo từng dòng đơn hàng trước khi cho phép xuất.
  - Sử dụng Transaction khi thực thi xuất kho:
    - Kiểm tra đơn hàng chưa từng có phiếu xuất (`!order.PHIEUXUATs.Any()`).
    - Kiểm tra sách đủ tồn kho (`Soluongton >= Soluong`).
    - Khấu trừ tồn kho `Soluongton` một lần duy nhất.
    - Tạo bản ghi trong bảng `PHIEUXUAT` gắn với `MaDH`.
- **Service Đơn hàng ([`OrderService.cs`](QuanlyNhaSach/Services/OrderService.cs)):**
  - Tạo đơn hàng khách hàng (`CreateCustomerOrder`): tạo hoặc liên kết khách hàng trong `NGUOIDUNG`, thêm `DONHANG` và các dòng `CHITIETDONHANG` trong 1 transaction.
  - Chuyển trạng thái đơn hàng (`TransitionOrderStatus`): kiểm tra điều kiện xuất kho trước khi cho chuyển sang trạng thái "Đang giao"; nếu hủy đơn hàng đã xuất kho, tự động hoàn trả tồn kho về cho bảng `SACH`.
  - Ghi nhận thanh toán (`RecordPayment`): cập nhật `Dathanhtoan`, phương thức thanh toán, số tiền khách đưa và tính tiền thối.
- **Presentation Layer:**
  - Đồng bộ toàn bộ các ViewModel:
    - [`StaffStockOutViewModel.cs`](QuanlyNhaSach/ViewModels/Staff/StaffStockOutViewModel.cs)
    - [`StaffOrderProcessingViewModel.cs`](QuanlyNhaSach/ViewModels/Staff/StaffOrderProcessingViewModel.cs)
    - [`StaffSalesViewModel.cs`](QuanlyNhaSach/ViewModels/Staff/StaffSalesViewModel.cs)
    - [`StaffDashboardViewModel.cs`](QuanlyNhaSach/ViewModels/Staff/StaffDashboardViewModel.cs)
    - [`StaffLookupViewModel.cs`](QuanlyNhaSach/ViewModels/Staff/StaffLookupViewModel.cs)
    - [`ManagerOverviewViewModel.cs`](QuanlyNhaSach/ViewModels/Manager/ManagerOverviewViewModel.cs)
    - [`CartCheckoutViewModel.cs`](QuanlyNhaSach/ViewModels/Customer/CartCheckoutViewModel.cs)
    - [`CustomerProfileViewModel.cs`](QuanlyNhaSach/ViewModels/Customer/CustomerProfileViewModel.cs) & Converter [`ValueConverters.cs`](QuanlyNhaSach/Converters/ValueConverters.cs).

---

### 2.5. Phân hệ Quản lý Người dùng & Xác thực (User & Auth Modules)
- **Service Quản lý Người dùng ([`UserManagementService.cs`](QuanlyNhaSach/Services/UserManagementService.cs)):**
  - Quản lý danh sách nhân viên (`NV...`) và khách hàng (`KH...`) từ bảng `NGUOIDUNG`.
  - Tự động sinh mã `MaND` (`QL001`, `NV001`, `KH001`...).
  - Xử lý khóa / mở khóa tài khoản (`Trangthai = "Hoạt động"` / `"Đã khóa"`), bảo vệ tài khoản Quản lý duy nhất không bị tự khóa.
  - Cấp lại mật khẩu ngẫu nhiên cho khách hàng và lưu trực tiếp vào CSDL.
- **Service Xác thực ([`AuthService.cs`](QuanlyNhaSach/Services/AuthService.cs)):**
  - Đăng nhập bằng `Tendangnhap` hoặc `Email`.
  - Đăng ký tài khoản khách hàng mới trực tiếp vào bảng `NGUOIDUNG`.
  - Tự động gieo dữ liệu tài khoản quản trị/nhân viên/khách hàng mặc định nếu CSDL trống.
- **Presentation Layer & Navigation:**
  - Đồng bộ [`AuthViewModel.cs`](QuanlyNhaSach/ViewModels/AuthViewModel.cs), [`StaffManagementViewModel.cs`](QuanlyNhaSach/ViewModels/Manager/StaffManagementViewModel.cs), [`CustomerManagementViewModel.cs`](QuanlyNhaSach/ViewModels/Manager/CustomerManagementViewModel.cs).
  - Cập nhật [`INavigationService.cs`](QuanlyNhaSach/Services/INavigationService.cs) và [`NavigationService.cs`](QuanlyNhaSach/Services/NavigationService.cs) nhận `NGUOIDUNG` thay cho `User`.
  - Đồng bộ các Main ViewModel: [`CustomerMainViewModel.cs`](QuanlyNhaSach/ViewModels/CustomerMainViewModel.cs), [`StaffMainViewModel.cs`](QuanlyNhaSach/ViewModels/StaffMainViewModel.cs), [`ManagerMainViewModel.cs`](QuanlyNhaSach/ViewModels/Manager/ManagerMainViewModel.cs).
  - Tinh giản [`App.xaml.cs`](QuanlyNhaSach/App.xaml.cs): loại bỏ sạch sẽ các dòng comment MockDataStore/MockDataService, khởi tạo các dịch vụ thực tế.

---

## 3. DANH SÁCH FILE THAY ĐỔI

### 3.1. Các file tạo mới
1. `QuanlyNhaSach/Models/NHACUNGCAP.Extended.cs`
2. `QuanlyNhaSach/Models/PHIEUNHAP.Extended.cs`
3. `QuanlyNhaSach/Models/DONHANG.Extended.cs`
4. `QuanlyNhaSach/Models/NGUOIDUNG.Extended.cs`
5. `DATABASE_INTEGRATION_UPDATE.md` (Tài liệu này)

### 3.2. Các file đã xóa vĩnh viễn (Mock / Deprecated Models)
1. `QuanlyNhaSach/Models/TemporaryCompatibilityModels.cs`
2. `QuanlyNhaSach/Models/User.cs`
3. `QuanlyNhaSach/Models/Customer.cs`
4. `QuanlyNhaSach/Models/Staff.cs`
5. `QuanlyNhaSach/Models/Manager.cs`

### 3.3. Các file cập nhật
- **Services:**
  - `QuanlyNhaSach/Services/SupplierService.cs`
  - `QuanlyNhaSach/Services/ImportService.cs`
  - `QuanlyNhaSach/Services/OrderService.cs`
  - `QuanlyNhaSach/Services/InventoryService.cs`
  - `QuanlyNhaSach/Services/UserManagementService.cs`
  - `QuanlyNhaSach/Services/AuthService.cs`
  - `QuanlyNhaSach/Services/INavigationService.cs`
  - `QuanlyNhaSach/Services/NavigationService.cs`
- **Models & Converters:**
  - `QuanlyNhaSach/Models/StockOutLineItem.cs`
  - `QuanlyNhaSach/Models/ManagerOverviewModels.cs`
  - `QuanlyNhaSach/Converters/ValueConverters.cs`
- **ViewModels:**
  - `QuanlyNhaSach/ViewModels/AuthViewModel.cs`
  - `QuanlyNhaSach/ViewModels/CustomerMainViewModel.cs`
  - `QuanlyNhaSach/ViewModels/StaffMainViewModel.cs`
  - `QuanlyNhaSach/ViewModels/Manager/ManagerMainViewModel.cs`
  - `QuanlyNhaSach/ViewModels/Manager/ManagerOverviewViewModel.cs`
  - `QuanlyNhaSach/ViewModels/Manager/SupplierManagementViewModel.cs`
  - `QuanlyNhaSach/ViewModels/Manager/ImportReceiptViewModel.cs`
  - `QuanlyNhaSach/ViewModels/Manager/ImportHistoryViewModel.cs`
  - `QuanlyNhaSach/ViewModels/Manager/StaffManagementViewModel.cs`
  - `QuanlyNhaSach/ViewModels/Manager/CustomerManagementViewModel.cs`
  - `QuanlyNhaSach/ViewModels/Staff/StaffDashboardViewModel.cs`
  - `QuanlyNhaSach/ViewModels/Staff/StaffLookupViewModel.cs`
  - `QuanlyNhaSach/ViewModels/Staff/StaffSalesViewModel.cs`
  - `QuanlyNhaSach/ViewModels/Staff/StaffOrderProcessingViewModel.cs`
  - `QuanlyNhaSach/ViewModels/Staff/StaffStockOutViewModel.cs`
  - `QuanlyNhaSach/ViewModels/Customer/CartCheckoutViewModel.cs`
  - `QuanlyNhaSach/ViewModels/Customer/CustomerProfileViewModel.cs`
- **Views & Entry Points:**
  - `QuanlyNhaSach/App.xaml.cs`
  - `QuanlyNhaSach/Views/Manager/SupplierManagementPage.xaml`
  - `QuanlyNhaSach/Views/Manager/ImportReceiptPage.xaml`

---

## 4. BẢO VỆ TẬP TIN ĐƯỢC CHỈ ĐỊNH (PROTECTED FILES)

Hai file thuộc phân hệ Sách được giữ nguyên bản 100%, đối chiếu mã băm SHA-256 hoàn toàn trùng khớp:

| File | SHA-256 Hash | Kết quả kiểm tra |
|---|---|---|
| `QuanlyNhaSach/Services/BookService.cs` | `F8F6B656C7F0A040B2A97B9FAC33EA400557D6CE47F21EA39CD9C124F770B8D8` | **Khớp 100% — Không chỉnh sửa** |
| `QuanlyNhaSach/ViewModels/Manager/BookManagementViewModel.cs` | `7385DD31F549B5602C19E7138590927C7BB579E09050576DCD1FB87AE60012E9` | **Khớp 100% — Không chỉnh sửa** |

---

## 5. KẾT QUẢ XÁC MINH BUILD

```text
Command: dotnet build --no-restore
Build succeeded.
    0 Error(s)
    2 Warning(s) (Cảnh báo CS1030 connection string mặc định của EF Core)
Time Elapsed: 00:00:21.94
```

---

## 6. HƯỚNG DẪN KIỂM THỬ NHANH (SMOKE TEST)

1. **Khởi động ứng dụng:**
   - Chạy ứng dụng từ Visual Studio hoặc `dotnet run`.
   - Cửa sổ đăng nhập xuất hiện với 3 tài khoản mặc định gieo sẵn trong CSDL:
     - **Quản lý:** `manager@example.com` / `Manager123!`
     - **Nhân viên:** `staff@example.com` / `Staff123!`
     - **Khách hàng:** `customer@example.com` / `Customer123!`
2. **Quy trình Quản lý Nhà cung cấp & Nhập kho:**
   - Đăng nhập quyền **Quản lý** -> Vào **Nhà cung cấp**: kiểm tra thêm mới/sửa nhà cung cấp.
   - Vào **Lập phiếu nhập**: chọn nhà cung cấp, chọn sách, nhập số lượng và đơn giá -> Bấm **Xác nhận nhập kho** -> Kiểm tra số lượng tồn kho của sách tăng trong CSDL.
   - Vào **Lịch sử nhập kho**: xem danh sách phiếu nhập vừa tạo.
3. **Quy trình Khách hàng Đặt hàng:**
   - Đăng nhập quyền **Khách hàng** -> Chọn sách đưa vào giỏ hàng -> Vào **Giỏ hàng & Thanh toán** -> Chọn COD hoặc Chuyển khoản -> Bấm **Đặt hàng ngay** -> Kiểm tra đơn hàng mới sinh trong bảng `DONHANG` và `CHITIETDONHANG`.
4. **Quy trình Nhân viên Xuất kho & Thu tiền:**
   - Đăng nhập quyền **Nhân viên** -> Vào **Xử lý đơn hàng**: bấm xác nhận đơn hàng (`Mới` -> `Đã xác nhận`).
   - Vào **Xuất kho**: bấm **Xác nhận xuất kho** -> Kiểm tra bản ghi mới trong `PHIEUXUAT` và tồn kho sách giảm tương ứng.
   - Vào **Bán hàng & Thu tiền**: nhập số tiền khách đưa -> Bấm **Ghi nhận thu tiền** -> Hoàn tất đơn hàng.
