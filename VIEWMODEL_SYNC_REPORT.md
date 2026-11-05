# Báo cáo Đồng bộ hóa ViewModel — QuanlyNhaSach

Nhánh (Branch): `ViewmodlesFix` · Ngày: 06-11-2026 · SDK: .NET SDK 10.0.402 (dự án nhắm mục tiêu `net8.0-windows`)

## 1. Tóm tắt tổng quan

Mục tiêu là đồng bộ Tầng Trình diễn (Presentation Layer - gồm ViewModels và các XAML bindings) với các thực thể EF Core thực tế được scaffold từ SQL Server, trong bối cảnh Tầng Dịch vụ (Service Layer) không còn sử dụng dữ liệu giả lập (mock data).

**Phát hiện chính:** Phía mã nguồn C# vốn dĩ đã có tính nhất quán cao. Bản build cơ sở ban đầu có **0 lỗi**, và toàn bộ các ViewModel đều đã sử dụng đúng các kiểu thực thể thực tế, tên thuộc tính chuẩn và đúng chữ ký của các Service. Điều này là bình thường vì trình biên dịch C# sẽ bắt ngay nếu có sự không khớp ở tầng mã lệnh. Rủi ro tiềm ẩn còn lại nằm ở **các liên kết XAML (XAML bindings)** – nơi mà trình biên dịch không thể kiểm tra. Một đường dẫn binding bị sai sẽ gặp lỗi ngầm trong lúc chạy (fails silently at runtime) và khiến ô bảng hoặc trường nhập liệu bị để trống.

Để tìm ra những lỗi đó, tôi đã xây dựng một bộ phân giải liên kết nhận biết ngữ cảnh (scope-aware binding resolver). Công cụ này ánh xạ từng View tới ViewModel tương ứng (từ hàm khởi tạo code-behind), phân giải từng `{Binding}` dựa trên kiểu DataContext thực tế và lần theo các đường dẫn phân cấp thuộc tính. Các kiểu DataContext mà công cụ xử lý gồm:
- ViewModel của trang (page ViewModel)
- Kiểu phần tử của tập hợp trong `ItemsSource`, áp dụng cho các cột DataGrid, cell template, row style và item template
- `DataContext.X` được truy xuất qua `RelativeSource AncestorType=Page`
- Thuộc tính tường minh `DataContext="{Binding …}"`

Tôi đã quét qua toàn bộ 20 Views và file `Resources/Templates.xaml`.

**Kết quả:** Đã phát hiện và sửa chữa 8 binding bị hỏng và 1 binding còn sót lại từ thời dùng mock-data. Bản build cuối cùng: **Build thành công — 0 Lỗi, 2 cảnh báo duy nhất** (bản ban đầu có 3 cảnh báo). Công cụ resolver hiện tại báo cáo **0 liên kết chưa được giải quyết (unresolved bindings)**.

## 2. Danh mục thực thể thực tế (Real Entity Inventory)

Tất cả các thực thể nằm trong namespace `QuanlyNhaSach.Models` và được lưu tại thư mục `QuanlyNhaSach/Models/`. Các cột được liệt kê chính xác như khai báo từ quá trình scaffold. Các thành viên mở rộng (`[NotMapped]`) được định nghĩa trong các file `*.Extended.cs`.

| Thực thể | Các thuộc tính scaffold (được ánh xạ DB) | Thuộc tính điều hướng (Navigations) | Các thành viên trình diễn `[NotMapped]` chính |
|---|---|---|---|
| `SACH` | `MaSach` (string, PK), `Tensach` string?, `Tacgia` string?, `NXB` string?, `Giaban` decimal?, `Soluongton` byte?, `Theloai` string?, `MaNCC` string?, `Daxoa` bool? | `MaNCCNavigation`, `CHITIETDONHANGs`, `CHITIETGIOHANGs`, `CHITIETPHIEUNHAPs` | `MauBia`, `AnhBia`, `MoTa`, `IsInStock`, `IsOutOfStock`, `FormattedPrice`, `StockStatusText`, `AdminStockStatusText` |
| `NHACUNGCAP` | `MaNCC` (string, PK), `TenNCC`, `Email`, `SDT`, `Diachi` (đều là string?) | `PHIEUNHAPs`, `SACHs` | `SupplierCode`, `DiaChi` (bọc `Diachi`), `IsActive`, `StatusText`, `ToggleStatusActionText`, `DisplayName` |
| `DONHANG` | `MaDH` (int, PK), `MaKH` string?, `NgayTao` DateOnly, `Phuongthucthanhtoan` string?, `Trangthaigiaohang` string?, `Dathanhtoan` bool?, `Tongtien` decimal? | `MaKHNavigation`, `CHITIETDONHANGs`, `PHIEUXUATs` | `OrderCode`, `HoTenKhachHang`, `SDTKhachHang`, `DiaChiGiaoHang`, `NgayDat` DateTime, `TongTien` decimal, `Status`, `PaymentStatus`, `StockOutStatus`, `Items`, `TrangThai*`, `Formatted*`, `CanRecordPayment`, `CanTransitionStatus`, `RequiredActionText` |
| `CHITIETDONHANG` | `MaDH` int, `MaSach` string (composite PK), `Soluong` byte?, `Tong` decimal? | `MaDHNavigation`, `MaSachNavigation` | `TenSach`, `TacGia`, `SoLuongMua`, `DonGia`, `ThanhTien`, `FormattedDonGia`, `FormattedThanhTien` |
| `PHIEUNHAP` | `MaPN` (int, PK), `MaQly` string?, `MaNCC` string?, `NgayNhap` DateOnly, `Dathanhtoan` bool?, `Tongtien` decimal? | `MaNCCNavigation`, `MaQlyNavigation`, `CHITIETPHIEUNHAPs` | `ReceiptCode`, `TenNCC`, `SdtNCC`, `DiaChiNCC`, `NgayLap`, `TongTien`, `TrangThai`, `NguoiLapHoTen`, `Details`, `BooksSummary`, `FormattedTongTien`, `FormattedNgayLap`, `TrangThaiLabel` |
| `CHITIETPHIEUNHAP` | `MaPN` int, `MaSach` string, `Soluongnhap` byte?, `Tong` decimal? | `MaPNNavigation`, `MaSachNavigation` | `BookCode`, `TenSach`, `TacGia`, `CurrentStock`, `SoLuongNhap`, `DonGiaNhap`, `ThanhTien`, `Formatted*` |
| `NGUOIDUNG` | `MaND` (string, PK), `Tendangnhap` string, `Matkhau` string, `Hoten` string?, `SDT` string?, `Email` string?, `Diachi` string?, `Vaitro` string?, `Trangthai` string? | `DONHANGs`, `GIOHANGs`, `PHIEUNHAPs` | `UserCode`, `Role`, `IsActive`, `RoleDisplayName`, `AccountStatusLabel`, `ToggleLockActionText`, `Initials` |
| `GIOHANG` | `MaGH` int, `MaKH` string?, `NgayCapNhat` DateTime? | `MaKHNavigation`, `CHITIETGIOHANGs` | — |
| `CHITIETGIOHANG` | `MaGH` int, `MaSach` string, `Soluong` byte?, `Tong` decimal? | `MaGHNavigation`, `MaSachNavigation` | — |
| `PHIEUXUAT` | `MaPX` int, `MaDH` int?, `Ghichu` string?, `NgayXuat` DateOnly | `MaDHNavigation` | — |

Các model tầng trình diễn (không phải thực thể DB) được ViewModels sử dụng: `CartItem`, `StockOutLineItem`, `DashboardStatistics`, `RevenueChartPoint`, `BestSellingBookItem`, `OperationResult<T>`.

**Lưu ý quan trọng về cách đặt tên:** Bản scaffold tự động viết chữ thường cho từ thứ hai ở một số cột (`Tensach`, `Tacgia`, `Giaban`, `Soluongton`, `Hoten`, `Diachi`, `Tendangnhap`). Một số class mở rộng lại chủ đích cung cấp các bí danh theo chuẩn PascalCase (`CHITIETDONHANG.TenSach`, `NHACUNGCAP.DiaChi`, `DONHANG.TongTien`). Kết quả là `TenSach` hợp lệ khi binding trong chi tiết đơn hàng nhưng **không hợp lệ** đối với `SACH`, và `DiaChi` hợp lệ với nhà cung cấp nhưng **không hợp lệ** đối với `NGUOIDUNG`. Tất cả các binding bị hỏng được tìm thấy trong đợt này đều xuất phát từ sự bất đối xứng này.

## 3. Các ViewModel đã cập nhật

| ViewModel | Thay đổi đã thực hiện | Trạng thái |
|---|---|---|
| `ViewModels/Customer/CartCheckoutViewModel.cs` | Bổ sung các thuộc tính `EmailError` / `HasEmailError` mà file `CartCheckoutPage.xaml` đã binding trước đó, kèm logic kiểm tra email trong `UpdateFieldErrors`, `CanConfirmCheckout`, bộ bảo vệ submit và `ResetFormFields`. Quy tắc kiểm tra đồng bộ với `OrderService.CreateCustomerOrder`: không được để trống, đuôi `@gmail.com`, có phần tên định danh trước @ và không chứa khoảng trắng. | Hoàn thành |

Tôi đã kiểm tra toàn bộ các ViewModel khác (18 file trong thư mục `ViewModels/`). Tất cả đều đã khớp chuẩn với các thực thể và hợp đồng Service nên được giữ nguyên.

## 4. Thay đổi về thuộc tính và kiểu dữ liệu

| Vị trí | Tham chiếu trước đây | Tham chiếu chính xác | Lý do |
|---|---|---|---|
| CustomerManagementPage / StaffManagementPage (kiểu dòng `NGUOIDUNG`) | `HoTen` | `Hoten` | Lệch chữ hoa/thường so với tên cột được scaffold |
| Tương tự | `Username` | `Tendangnhap` | Không tồn tại thành viên này; tên cột đăng nhập chuẩn là `Tendangnhap` |
| Tương tự | `DiaChi` | `Diachi` | `NGUOIDUNG` không có bí danh `DiaChi` (chỉ `NHACUNGCAP` mới có) |
| CartCheckoutPage (`CartCheckoutViewModel`) | `EmailError`, `HasEmailError` (bị thiếu) | Khai báo dạng `[ObservableProperty]` kèm thuộc tính bool tính toán | XAML đã liên kết tới các thành viên không tồn tại trong ViewModel |
| CustomerProfilePage bảng đơn hàng (kiểu dòng `DONHANG`) | `MaDH` → `CustomerOrderTotalConverter` | `TongTien` (decimal) → cùng converter | Tồn dư từ thời dùng dữ liệu mẫu. Việc truyền ID khiến converter gọi hàm `GetMockOrderTotal(maDh)`, gây truy vấn DB riêng cho mỗi dòng không có cache. Giờ đây tổng tiền thực tế kiểu `decimal` đã được load sẵn và binding trực tiếp. |

Kiểu dữ liệu: Tiền tệ luôn là `decimal`/`decimal?` ở mọi nơi; không có ViewModel nào dùng `double` cho số tiền. Ngày tháng giữ nguyên `DateOnly`/`DateTime` và chỉ được định dạng thông qua converter hoặc các thuộc tính `Formatted*`. Tồn kho (`byte?`) được hỗ trợ chuẩn bởi `StockBadge*Converter`, và `DateOnly` được hỗ trợ chuẩn bởi `CustomerOrderDateConverter`.

## 5. Các file XAML đã chỉnh sửa

- `Views/Manager/CustomerManagementPage.xaml`: Sửa các cột họ tên (cell template), tên đăng nhập và địa chỉ.
- `Views/Manager/StaffManagementPage.xaml`: Sửa 3 cột tương tự.
- `Views/Customer/CustomerProfilePage.xaml`: Cột tổng tiền đơn hàng chuyển sang binding trực tiếp với `TongTien`.
- `Views/Customer/CartCheckoutPage.xaml`: **Không chỉnh sửa**. Các binding `EmailError` / `HasEmailError` trong file này vốn đã viết đúng; nguyên nhân là do ViewModel trước đó bị thiếu thuộc tính.

Không có ResourceDictionary nào cần can thiệp. `Templates.xaml` (với DataType là `SACH` và `DONHANG`) vốn đã sử dụng các tên chuẩn (`Tensach`, `Tacgia`, `Soluongton`, `Trangthaigiaohang`).

## 6. Đồng bộ hợp đồng dịch vụ (Service Contract Synchronization)

Các Service là các class cụ thể được đăng ký trực tiếp không qua interface (`BookService`, `SupplierService`, `OrderService`, `ImportService`, `InventoryService`, `UserManagementService`, `CartService`, `AuthService`). Tất cả đều hoạt động **đồng bộ (synchronous)** và trả về các thực thể EF, `IReadOnlyList<T>`, `OperationResult` / `OperationResult<T>`, hoặc các DTO trình diễn (`DashboardStatistics`, `RevenueChartPoint`, `BestSellingBookItem`, `StockOutLineItem`).

Tất cả các collection trong ViewModel đều tương thích hoàn toàn với kiểu phần tử trả về từ Service tương ứng, ví dụ:
- `ObservableCollection<SACH>` ← `BookService.SearchAndFilterBooks`
- `ObservableCollection<NGUOIDUNG>` ← `UserManagementService.SearchAccounts`
- `ObservableCollection<StockOutLineItem>` ← `InventoryService.GetStockOutLineItems`
- `ObservableCollection<BestSellingBookItem>` ← `OrderService.GetTopSellingBooks`

Không có Service nào bị sửa đổi. Không có ViewModel nào gây nghẽn luồng UI bằng `.Result` hay `.Wait()`. Do bản thân các Service đã là đồng bộ nên không cần chuyển đổi bất đồng bộ.

## 7. Kết quả Build

Lệnh thực thi: `dotnet restore QuanlyNhaSach.sln` sau đó `dotnet build QuanlyNhaSach.sln` (dự án đơn nhất: `QuanlyNhaSach/QuanlyNhaSach.csproj`).

| | Lỗi (Errors) | Cảnh báo duy nhất (Unique warnings) |
|---|---|---|
| Cơ sở ban đầu (trước thay đổi) | 0 | 3 — `CS8604` AuthService.cs:189, `CS8602` AuthService.cs:278, `CS0414` CartCheckoutViewModel.cs:29 |
| Sau đợt 1 (các bảng tài khoản) | 0 | 3 |
| Kết quả cuối cùng | **0** | **2** — gồm hai cảnh báo nullable đã có từ trước trong `AuthService`. Cảnh báo `CS0414` đã được xử lý xong do biến `_hasUserTouchedEmail` nay đã được sử dụng. |

MSBuild ghi nhận mỗi cảnh báo hai lần do WPF tiến hành biên dịch một bản tạm thời `*_wpftmp.csproj`, do đó số đếm thô giảm từ 6 → 4.

**Build thành công — 0 Lỗi.**

## 8. Xác minh Binding tại thời điểm chạy (Runtime Verification)

| Hạng mục kiểm tra | Trạng thái |
|---|---|
| Kiểm tra XAML tĩnh (resolver nhận biết phạm vi trên toàn bộ Views và Templates.xaml) | **Đã hoàn thành**: 0 đường dẫn chưa giải quyết sau khi sửa lỗi |
| Liên kết lệnh (Command bindings) (`*Command`, gồm cả `DataContext.*Command` qua RelativeSource) | **Đã hoàn thành tĩnh**: Tất cả đều liên kết chính xác tới các command được sinh tự động bởi `[RelayCommand]` trên ViewModel chủ quản |
| Khởi chạy GUI, nạp dữ liệu DataGrid, thực thi lệnh | **Chưa thực hiện.** Ứng dụng yêu cầu có SQL Server đang chạy và cần thao tác tương tác qua từng vai trò (khách hàng, nhân viên, quản lý). Phần này vẫn còn tồn đọng. |
| Chẩn đoán binding WPF trong đầu ra Debug (`System.Windows.Data Error: 40`) | **Chưa thực hiện.** Khuyến nghị: Chạy ứng dụng dưới chế độ gỡ lỗi (Debugger) của Visual Studio, mở từng trang và theo dõi cửa sổ Output. |

Hạn chế của resolver tĩnh: Công cụ không đánh giá các bộ chuyển đổi (converter), `MultiBinding`, hoặc binding theo `ElementName`, và coi các item template của `ComboBox` liên kết với mảng `string` là chưa định kiểu. Không có view nào phụ thuộc vào các trường hợp này để hiển thị dữ liệu thực thể.

## 9. Danh sách các file đã tạo hoặc chỉnh sửa

- Đã sửa `QuanlyNhaSach/ViewModels/Customer/CartCheckoutViewModel.cs`
- Đã sửa `QuanlyNhaSach/Views/Manager/CustomerManagementPage.xaml`
- Đã sửa `QuanlyNhaSach/Views/Manager/StaffManagementPage.xaml`
- Đã sửa `QuanlyNhaSach/Views/Customer/CustomerProfilePage.xaml`
- Đã cập nhật `VIEWMODEL_SYNC_REPORT.md`

Không có nội dung nào được commit hay push git. Không có thay đổi schema database, không có migration và không tạo lại scaffold.

## 10. Các vấn đề tồn đọng cần lưu ý

1. **`CustomerProfileViewModel` truy cập trực tiếp cơ sở dữ liệu** (`new QuanlyNhaSachContext()` tại 3 vị trí: tải hồ sơ/đơn hàng, `GetMockOrderTotal`, và `UpdateProfile`). Việc này vi phạm mô hình phân tầng MVVM / Service. Đây là ViewModel duy nhất mắc lỗi này. Việc tách logic này sang Service thuộc phạm vi tái cấu trúc (refactoring) ngoài phạm vi đồng bộ hóa binding, nên hiện được giữ nguyên.
2. **Khả năng lộ thông tin trong ViewModel nói trên:** Khi khách hàng đăng nhập chưa có đơn hàng nào, phương thức `LoadCustomerAndOrdersFromDatabase` lại tự động hiển thị 10 đơn hàng mới nhất của *toàn bộ* khách hàng khác. Ngoài ra phương thức này cũng tự thế dữ liệu mẫu hard-code (`KH001`, "Nguyễn Minh Anh") khi không tìm thấy phiên người dùng. Đây là các vấn đề về logic nghiệp vụ và chưa can thiệp trong đợt này.
3. `CustomerProfileViewModel.GetMockOrderTotal` bị đặt tên sai lệch: Thực chất hàm này đọc tổng tiền thực từ database. Hiện tại hàm này chỉ được gọi khi converter rơi vào nhánh dự phòng (fallback).
4. Logic kiểm tra email ở phía server trong `OrderService.CreateCustomerOrder` chấp nhận cả chuỗi trần `@gmail.com` (chỉ cần độ dài ≥ 10) và không loại bỏ khoảng trắng đầu/cuối. Quy tắc mới ở phía client chặt chẽ hơn, nên dữ liệu gửi lên hợp lệ vẫn được xử lý bình thường.
5. Hai cảnh báo nullable có từ trước trong `AuthService.cs` (dòng 189 và 278) vẫn còn. Chúng không thuộc phạm vi công việc lần này.
6. Việc kiểm thử runtime và giao diện người dùng thực tế (GUI) vẫn cần được hoàn thiện thêm (xem mục 8).
