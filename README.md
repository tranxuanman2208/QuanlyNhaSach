# BookNest (QuanlyNhaSach) — Hệ Thống Quản Lý Nhà Sách WPF (Customer, Staff & Manager / Admin Portals)

Ứng dụng Desktop **BookNest — Nhà Sách Nhóm 1 (`QuanlyNhaSach`)** được phát triển bằng **WPF (.NET 8)**, **C#**, **XAML**, kiến trúc **MVVM** và **CommunityToolkit.Mvvm**, tích hợp đầy đủ **3 phân hệ (Customer Portal, Staff Portal, Manager / Admin Portal)** trên một nguồn dữ liệu dùng chung duy nhất trong bộ nhớ (**`MockDataStore` — 100% In-Memory Mock Data**).

---

## 1. Tài Khoản Mẫu Kiểm Thử (Demo-Only Credentials)

> **Cảnh báo phạm vi bảo mật (Demo-Only Security Scope):**
> Hệ thống xác thực và mật khẩu dưới đây chỉ là dữ liệu mẫu lưu trong bộ nhớ (`MockDataStore`) nhằm phục vụ kiểm thử giao diện và luồng nghiệp vụ độc lập, **không phải** hệ thống bảo mật môi trường Production (vốn yêu cầu băm mật khẩu an toàn, xác thực phía máy chủ và cơ sở dữ liệu thực tế).

| Vai trò (`UserRole`) | Tên đăng nhập (`Username`) | Mật khẩu (`Password`) | Họ và tên | Trạng thái | Giao diện điều hướng |
|---|---|---|---|---|---|
| **Manager** (Quản lý / Admin) | `manager@example.com` | `Manager123!` | Lê Quản Trị | Hoạt động | `ManagerMainWindow` |
| **Staff** (Nhân viên) | `staff@example.com` | `Staff123!` | Trần Thị Minh Thư | Hoạt động | `StaffMainWindow` |
| **Customer** (Khách hàng 1) | `customer@example.com` | `Customer123!` | Nguyễn Văn An | Hoạt động | `CustomerMainWindow` |
| **Customer** (Khách hàng 2) | `lehoangnam@example.com` | `Customer123!` | Lê Hoàng Nam | Hoạt động | `CustomerMainWindow` |
| **Staff** (Khóa) | `nv.quochuy@example.com` | `Staff123!` | Ngô Quốc Huy | Đã khóa (`IsActive = false`) | Từ chối đăng nhập |
| **Customer** (Khóa) | `locked@example.com` | `Locked123!` | Phạm Quốc Bảo | Đã khóa (`IsActive = false`) | Từ chối đăng nhập |

- Tại màn hình `AuthWindow`, bạn có thể nhập trực tiếp thông tin trên hoặc nhấn các nút **TK Khách Hàng**, **TK Nhân Viên**, **TK Quản Lý** ở khung bên trái để điền nhanh khi kiểm thử.
- Chức năng **Đăng ký tài khoản mới (UC14)** chỉ cho phép tạo tài khoản **Khách hàng (`Customer`)**, nghiêm cấm tự đăng ký quyền Nhân viên (`Staff`) hoặc Quản lý (`Manager`).

---

## 2. Giới Hạn Dữ Liệu Mô Phỏng (In-Memory Mock Data Limitations)

- Toàn bộ dữ liệu **Sách (`Books` — 22 đầu sách)**, **Người dùng (`Users` — 28 tài khoản: 1 Quản lý, 11 Nhân viên, 16 Khách hàng)**, **Nhà cung cấp (`Suppliers` — 6 nhà cung cấp)**, **Phiếu nhập hàng (`ImportReceipts` — 6 phiếu nhập lịch sử với 18 dòng chi tiết)** và **Đơn hàng (`Orders` — 16 đơn hàng)** được quản lý tập trung qua một thể hiện Singleton duy nhất của `MockDataStore` trong suốt phiên chạy ứng dụng.
- Không sử dụng SQL Server, Entity Framework Core, Dapper hay REST API. File `DBQuanLyNhaSach.sql` được giữ nguyên để tham khảo.
- Mọi thao tác giữa **Quản lý**, **Nhân viên** và **Khách hàng** trong cùng một phiên làm việc (thêm/sửa sách, lập phiếu nhập cộng tồn kho, khóa/mở khóa tài khoản, cấp lại mật khẩu tạm thời, khách đặt hàng, nhân viên xuất kho & thu tiền) đều đồng bộ tức thì trên cùng các đối tượng dữ liệu trong RAM.
- **Lưu ý:** Dữ liệu chỉ tồn tại trong phiên chạy hiện tại; khi đóng hoàn toàn ứng dụng và khởi chạy lại, dữ liệu sẽ khôi phục về trạng thái khởi tạo mẫu ban đầu.

---

## 3. Các Chức Năng Cổng Quản Trị (`ManagerMainWindow`)

Thanh điều hướng bên trái (`#172B4D`) của `ManagerMainWindow` gồm đúng 7 mục theo thứ tự:

1. **Tổng quan (`ManagerOverviewPage` — UC13)**:
   - Hiển thị toàn bộ Dashboard quản trị trong một trang thống nhất:
   - **4 thẻ thống kê**: Tổng doanh thu thực thu (chỉ tính đơn `Hoàn tất` & `Đã thanh toán`), Tổng đơn hàng, Sách bán chạy nhất, Sách sắp hết kho (`SoLuongTon < 10`, bao gồm cả sách tồn bằng `0`).
   - **Biểu đồ cột Doanh thu theo thời gian** (`7 ngày gần nhất` / `14 ngày gần nhất`) với sắc cam chủ đạo `#E85D22`.
   - **Top 5 sách bán chạy nhất** xếp hạng theo số lượng thực bán từ các đơn hoàn tất.
   - **Bảng cảnh báo sách sắp hết kho (`SoLuongTon < 10`)** kèm lối tắt mở Quản lý sách hoặc Lập phiếu nhập hàng.
2. **Quản lý sách (`BookManagementPage` — UC09)**:
   - Tìm kiếm, lọc theo trạng thái tồn kho (`Đủ hàng >= 10`, `Sắp hết < 10`, `Hết hàng = 0`, `Ngừng bán`).
   - Thêm sách mới, chỉnh sửa thông tin sách (cập nhật tức thì sang danh mục Khách hàng mà không làm sai lệch snapshot đơn hàng/phiếu nhập cũ).
   - Xóa hoặc chuyển sang `Ngừng bán` an toàn khi sách đã được tham chiếu trong lịch sử đơn hàng/phiếu nhập.
3. **Quản lý nhân viên (`StaffManagementPage` — UC02)**:
   - Tìm kiếm theo Mã NV, Họ tên, Tên đăng nhập, Số điện thoại; lọc theo `Tất cả`, `Hoạt động`, `Đã khóa`.
   - Thêm tài khoản nhân viên mới (tự động gán vai trò `Staff`, kiểm tra trùng `Username`).
   - Khóa / Mở khóa tài khoản nhân viên có xác nhận (tác động tức thì tới `AuthService.Login`).
4. **Quản lý khách hàng (`CustomerManagementPage`)**:
   - Tìm kiếm và lọc trạng thái tài khoản khách hàng.
   - Khóa / Mở khóa tài khoản khách hàng (bảo toàn lịch sử đơn hàng).
   - **Cấp lại mật khẩu (Mô phỏng)**: Xác nhận trước khi sinh mật khẩu tạm thời mới, cập nhật vào tài khoản và chỉ hiển thị 1 lần trong hộp thoại kết quả.
5. **Nhà cung cấp (`SupplierManagementPage` — UC12)**:
   - Tìm kiếm, thêm mới, chỉnh sửa nhà cung cấp (kiểm tra định dạng SĐT và Email).
   - Ngừng hợp tác / Kích hoạt lại an toàn để bảo vệ toàn vẹn tham chiếu của các phiếu nhập lịch sử.
6. **Nhập hàng (`ImportReceiptPage`)**:
   - Chọn nhà cung cấp đang hoạt động, tìm kiếm sách và thêm vào phiếu nhập nháp.
   - Chỉnh sửa trực tiếp `SoLuongNhap` và `DonGiaNhap` với tính toán tự động thời gian thực cho `ThanhTien = SoLuongNhap * DonGiaNhap` và `TongTien`.
   - **Quy tắc tồn kho nghiêm ngặt**: Phiếu nháp không làm tăng tồn kho; chỉ khi nhấn `Xác nhận nhập hàng` và vượt qua toàn bộ kiểm tra hợp lệ thì tồn kho `SoLuongTon` mới được cộng thêm nguyên tử (atomic).
7. **Lịch sử nhập (`ImportHistoryPage`)**:
   - Tra cứu các phiếu nhập đã xác nhận theo Mã phiếu nhập hoặc Nhà cung cấp.
   - Nhấn `Xem chi tiết` để xem đầy đủ bản chụp lịch sử bất biến (số lượng nhập, đơn giá nhập gốc, thành tiền và tổng tiền).

---

## 4. Hướng Dẫn Biên Dịch & Chạy Ứng Dụng

Tại thư mục gốc chứa `QuanlyNhaSach.sln`, chạy các lệnh sau trên PowerShell hoặc Command Prompt:

```powershell
dotnet restore QuanlyNhaSach.sln
dotnet build QuanlyNhaSach.sln
dotnet run --project QuanlyNhaSach\QuanlyNhaSach.csproj
```
