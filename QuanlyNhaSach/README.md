# BookNest (QuanlyNhaSach) — Hệ Thống Quản Lý & Bán Sách Trực Tuyến

Ứng dụng Desktop **BookNest (`QuanlyNhaSach`)** được xây dựng trên nền tảng **WPF (.NET 8)**, ngôn ngữ **C#**, giao diện **XAML** hiện đại theo mô hình kiến trúc **MVVM (CommunityToolkit.Mvvm)**. Hệ thống tích hợp toàn diện cơ sở dữ liệu **SQL Server (Entity Framework Core)**, hỗ trợ mã hóa mật khẩu an toàn bằng **BCrypt**, gửi mã xác thực OTP đăng ký qua **Gmail SMTP**, và sinh mã **VietQR** tự động khi thanh toán chuyển khoản ngân hàng.

---

## 1. Danh Sách Tài Khoản Kiểm Thử (Test Credentials)

Hệ thống được cài đặt sẵn 3 tài khoản tương ứng với 3 phân hệ chính của chương trình:

| Phân hệ / Vai trò | Tên đăng nhập (`UserName`) | Mật khẩu (`Pass`) | Họ và tên | Cổng điều hướng | Quyền hạn & Chức năng chính |
|---|---|---|---|---|---|
| **Quản lý** *(Manager)* | `manager123` | `manager123` | Trần Khởi Vy | `ManagerMainWindow` | Xem báo cáo doanh thu, biểu đồ phân tích, quản lý sách, nhân viên, khách hàng, nhà cung cấp, nhập kho & lịch sử nhập. |
| **Nhân viên** *(Staff)* | `staff123` | `staff123` | Nguyễn Huyền Quang | `StaffMainWindow` | Bán hàng tại quầy (POS), tra cứu kho & giá sách, xử lý tiếp nhận đơn online, lập phiếu xuất kho. |
| **Khách hàng** *(Customer)* | `customer12` | `customer123` | Trần Huy Hoàng | `CustomerMainWindow` | Duyệt danh mục sách, tìm kiếm, giỏ hàng, đặt hàng thanh toán VietQR / COD, xem lịch sử đơn & quản lý hồ sơ. |

> **Ghi chú đăng nhập:**
> - Bạn có thể đăng nhập bằng **Tên đăng nhập** (`UserName`) hoặc **Địa chỉ Email** đã liên kết với tài khoản.
> - Tài khoản sau khi đăng nhập thành công sẽ tự động được điều hướng đến đúng giao diện chức năng tương ứng với quyền hạn của mình.

---

## 2. Hướng Dẫn Cài Đặt & Khởi Chạy Ứng Dụng

### 2.1. Yêu cầu môi trường
- **Hệ điều hành:** Windows 10 / Windows 11 (64-bit).
- **.NET SDK:** Phiên bản **.NET 8.0 SDK** trở lên.
- **Cơ sở dữ liệu:** **Microsoft SQL Server** (bản 2017 trở lên hoặc SQL Server Express).
- **Công cụ phát triển khuyến nghị:** Visual Studio 2022 (với workload *.NET Desktop Development*) hoặc VS Code / JetBrains Rider.

### 2.2. Khởi tạo Cơ sở dữ liệu SQL Server
1. Mở **SQL Server Management Studio (SSMS)** và kết nối vào máy chủ SQL của bạn.
2. Mở file script `DBQuanLyNhaSach.sql` trong thư mục này.
3. Thực thi (Execute - `F5`) toàn bộ script để tạo Database `QuanlyNhaSach`, cấu trúc bảng và nạp sẵn dữ liệu mẫu (sách, tài khoản kiểm thử, phiếu nhập và đơn hàng).
4. Kiểm tra chuỗi kết nối trong file [QuanLyNhaSachContext.cs](file:///QuanlyNhaSach/Models/QuanLyNhaSachContext.cs):
   ```csharp
   private static readonly string[] CandidateConnectionStrings =
   {
       "Data Source=TRANMAN\\MSSQLSERVER02;Initial Catalog=QuanlyNhaSach;Integrated Security=True;Encrypt=True;Trust Server Certificate=True; Connection Timeout = 1;"
   };
   ```
   *Nếu tên máy chủ SQL của bạn khác `TRANMAN\MSSQLSERVER02`, vui lòng sửa lại `Data Source` cho khớp với máy cá nhân của bạn (ví dụ: `Data Source=.;Initial Catalog=QuanlyNhaSach;...` hoặc `Data Source=localhost;...`).*

### 2.3. Cấu hình tính năng mở rộng (Tùy chọn)
- **Gửi email xác thực OTP:** File cấu hình `emailsettings.json`:
  - Cho phép cấu hình Gmail và App Password (Mật khẩu ứng dụng 16 ký tự) để gửi mã OTP khi khách hàng đăng ký tài khoản mới.
- **Thanh toán chuyển khoản VietQR:** File cấu hình `banksettings.json`:
  - Cấu hình số tài khoản ngân hàng, mã ngân hàng (`MB`, `VCB`, `ACB`,...) để hệ thống tự sinh mã QR thanh toán chuẩn Napas.

### 2.4. Biên dịch và Chạy ứng dụng
Mở cửa sổ dòng lệnh (PowerShell / Terminal) tại thư mục gốc:

```powershell
# 1. Khôi phục thư viện dependencies
dotnet restore ..\QuanlyNhaSach.sln

# 2. Biên dịch toàn bộ Solution
dotnet build ..\QuanlyNhaSach.sln

# 3. Khởi chạy ứng dụng BookNest
dotnet run --project .\QuanlyNhaSach.csproj
```

---

## 3. Hướng Dẫn Sử Dụng Chi Tiết Từng Phân Hệ

---

### PHÂN HỆ XÁC THỰC — `AuthWindow`

Khi khởi động chương trình, màn hình xác thực **BookNest** xuất hiện:
- **Đăng nhập (UC01):**
  1. Nhập **Tên đăng nhập** (`UserName`) và **Mật khẩu** (`Pass`).
  2. Bấm nút **"Đăng nhập hệ thống"** (hoặc nhấn phím `Enter`).
  3. Hệ thống kiểm tra mật khẩu đã được mã hóa BCrypt, kiểm tra trạng thái hoạt động của tài khoản và tự động mở đúng cửa sổ làm việc của phân quyền đó.
- **Đăng ký tài khoản khách hàng mới (UC14):**
  1. Nhấn nút **"Đăng ký ngay"** ở chân form đăng nhập.
  2. Nhập các trường bắt buộc: *Họ và tên*, *Số điện thoại (10 chữ số)*, *Địa chỉ Email (@gmail.com)*, *Địa chỉ liên hệ*, *Tên đăng nhập (3-10 ký tự)*, *Mật khẩu* và *Xác nhận mật khẩu*.
  3. Bấm nút **"Gửi mã"** để nhận OTP qua email, nhập mã 6 số vào ô xác thực.
  4. Bấm **"Hoàn tất đăng ký"** để tạo tài khoản Khách hàng mới.

---

### PHÂN HỆ 1: KHÁCH HÀNG — `CustomerMainWindow`
*(Kiểm thử bằng tài khoản: `customer12` / `customer123`)*

Giao diện mua sắm sách trực quan, thân thiện, gồm các tính năng chính:

1. **Trang chủ (`HomePage`)**:
   - **Thanh tìm kiếm & Danh mục:** Tìm kiếm sách theo tên sách, tác giả, thể loại.
   - **Banner giới thiệu & Kệ sách:** Xem danh sách sách nổi bật, sách mới phát hành, sách bán chạy nhất.
   - **Thẻ sách:** Hiển thị bìa sách, tên sách, giá niêm yết, số lượng tồn và trạng thái còn hàng/hết hàng.
   - **Thao tác nhanh:** Bấm vào thẻ để xem chi tiết hoặc bấm biểu tượng giỏ hàng để thêm nhanh.

2. **Chi tiết sách (`BookDetailPage`)**:
   - Hiển thị đầy đủ thông tin: Tác giả, Thể loại, Nhà xuất bản, Năm xuất bản, Số trang, Mô tả tóm tắt nội dung sách.
   - Hiển thị tình trạng kho hàng theo thời gian thực.
   - Tùy chọn số lượng muốn mua:
     - Bấm **"Thêm vào giỏ hàng"** để tiếp tục mua sắm.
     - Bấm **"Mua ngay"** để chuyển thẳng tới bước thanh toán.

3. **Giỏ hàng & Đặt hàng (`CartCheckoutPage`)**:
   - **Quản lý giỏ hàng:** Xem danh sách sản phẩm đã chọn, điều chỉnh số lượng (nút `+` / `-`), xóa sản phẩm khỏi giỏ, tự động tính tổng tiền.
   - **Thông tin nhận hàng:** Nhập địa chỉ giao hàng và số điện thoại liên hệ nhận hàng.
   - **Phương thức thanh toán:**
     - *Thanh toán khi nhận hàng (COD / Tiền mặt)*: Đơn hàng ghi nhận trạng thái chờ xử lý.
     - *Chuyển khoản trực tuyến (VietQR)*: Hệ thống hiển thị mã QR động được sinh chuẩn VietQR với đúng số tài khoản, tên chủ thẻ, số tiền đơn hàng và cú pháp nội dung chuyển tiền.
   - Bấm **"Xác nhận đặt hàng"** để hoàn tất đơn mua.

4. **Trang cá nhân & Lịch sử đơn hàng (`CustomerProfilePage`)**:
   - Quản lý thông tin tài khoản: Xem và cập nhật họ tên, số điện thoại, địa chỉ nhận hàng mặc định.
   - **Đổi mật khẩu:** Cập nhật mật khẩu bảo vệ tài khoản.
   - **Lịch sử mua hàng:** Tra cứu danh sách đơn hàng đã đặt kèm mã đơn, thời gian, tổng tiền và trạng thái đơn hàng (*Chờ xác nhận*, *Đang xử lý*, *Đang giao hàng*, *Hoàn tất*, *Đã hủy*).

---

### PHÂN HỆ 2: NHÂN VIÊN NHÀ SÁCH — `StaffMainWindow`
*(Kiểm thử bằng tài khoản: `staff123` / `staff123`)*

Cổng làm việc nghiệp vụ hàng ngày của nhân viên tại cửa hàng:

1. **Bảng điều khiển (`StaffDashboardPage`)**:
   - Thống kê nhanh ca làm việc: Tổng đơn hoàn tất trong ngày, doanh số bán hàng, số đơn hàng trực tuyến đang chờ xử lý.
   - Lối tắt nhanh đến các phân hệ tác nghiệp.

2. **Bán hàng tại quầy / POS (`StaffSalesPage`)**:
   - Hỗ trợ tạo hóa đơn bán lẻ trực tiếp cho khách mua tại cửa hàng.
   - Tìm kiếm sách theo mã sách, tên sách, tác giả; thêm vào danh sách tính tiền.
   - Nhập số lượng mua; hệ thống tự động kiểm tra số lượng tồn kho khả dụng để ngăn chặn bán vượt số lượng tồn.
   - Tính toán tiền hàng, thuế, số tiền khách đưa và tiền thối lại cho khách.
   - Hỗ trợ thanh toán tiền mặt hoặc quét mã VietQR thanh toán tại quầy.
   - Bấm **"Hoàn tất bán hàng"** để trừ tồn kho tức thì và xuất hóa đơn.

3. **Tra cứu sách & Tồn kho (`StaffLookupPage`)**:
   - Tra cứu nhanh sách trong kho theo nhiều tiêu chí: Tên sách, Tác giả, Thể loại, Vị trí kệ trưng bày, Số lượng tồn kho và Giá bán.
   - Hỗ trợ nhân viên tư vấn khách hàng nhanh chóng và chính xác.

4. **Xử lý đơn hàng trực tuyến (`StaffOrderProcessingPage`)**:
   - Tiếp nhận danh sách các đơn hàng do Khách hàng đặt trực tuyến từ hệ thống.
   - Xem chi tiết từng món hàng trong đơn, thông tin người nhận và phương thức thanh toán.
   - Thực hiện thao tác tiếp nhận và chuyển trạng thái đơn hàng: Duyệt đơn, chuẩn bị đóng gói hoặc hủy đơn nếu có yêu cầu.

5. **Lập phiếu xuất kho (`StaffStockOutPage`)**:
   - Thực hiện quy trình xuất kho thực tế theo các đơn hàng đã duyệt.
   - Kiểm tra số lượng sách thực xuất và đối chiếu tồn kho.
   - Xác nhận xuất kho để chính thức bàn giao đơn cho đơn vị vận chuyển hoặc khách hàng.

---

### PHÂN HỆ 3: QUẢN LÝ / QUẢN TRỊ — `ManagerMainWindow`
*(Kiểm thử bằng tài khoản: `manager123` / `manager123`)*

Cổng quản trị toàn diện dành cho Quản lý / Chủ cửa hàng gồm 7 mục điều hướng:

1. **Tổng quan & Báo cáo (`ManagerOverviewPage`)**:
   - **4 thẻ chỉ số quan trọng (KPIs):** Doanh thu thực thu, Tổng số đơn hàng thành công, Sách bán chạy nhất, Sách sắp hết kho.
   - **Biểu đồ cột trực quan:** Doanh thu bán hàng theo thời gian (chu kỳ 7 ngày hoặc 14 ngày gần nhất).
   - **Bảng Top 5 sách bán chạy nhất:** Thống kê theo số lượng bán ra thực tế.
   - **Bảng cảnh báo tồn kho thấp (`SoLuongTon < 10`):** Giúp quản lý kịp thời lên kế hoạch nhập thêm hàng.

2. **Quản lý danh mục sách (`BookManagementPage`)**:
   - Xem bảng danh sách đầy đủ tất cả các đầu sách trong hệ thống.
   - Lọc theo thể loại, tìm kiếm theo tên/tác giả, lọc theo trạng thái kho (*Đủ hàng*, *Sắp hết*, *Hết hàng*, *Ngừng kinh doanh*).
   - **Thêm sách mới:** Nhập tên sách, tác giả, thể loại, nhà xuất bản, năm xuất bản, giá bán, số trang, hình ảnh bìa sách.
   - **Chỉnh sửa sách:** Cập nhật thông tin chi tiết hoặc điều chỉnh giá bán niêm yết.
   - **Ngừng bán / Xóa sách:** Quản lý trạng thái kinh doanh an toàn, bảo toàn lịch sử hóa đơn cũ.

3. **Quản lý nhân viên (`StaffManagementPage`)**:
   - Quản lý danh sách nhân viên: Mã NV, Họ tên, Tên đăng nhập, Số điện thoại, Email, Trạng thái hoạt động.
   - **Thêm nhân viên mới:** Cấp tài khoản nhân viên mới vào hệ thống (tự động phân quyền `Staff`).
   - **Khóa / Mở khóa tài khoản:** Tạm khóa tài khoản nhân viên đã nghỉ hoặc vi phạm chính sách; tác động tức thì tới quyền đăng nhập của nhân viên.
   - **Cấp lại mật khẩu:** Hỗ trợ đặt lại mật khẩu cho nhân viên khi quên mật khẩu.

4. **Quản lý khách hàng (`CustomerManagementPage`)**:
   - Xem danh sách toàn bộ khách hàng đã đăng ký tài khoản.
   - Tra cứu khách hàng theo họ tên, số điện thoại hoặc email.
   - Khóa / Mở khóa tài khoản khách hàng khi phát hiện gian lận.
   - Xem tổng số đơn hàng và tổng chi tiêu của từng khách hàng.

5. **Quản lý nhà cung cấp (`SupplierManagementPage`)**:
   - Quản lý danh sách các đối tác, nhà xuất bản, nhà phân phối cung ứng sách.
   - Thêm mới, chỉnh sửa thông tin nhà cung cấp (Tên công ty, SĐT, Email, Địa chỉ).
   - Quản lý trạng thái hợp tác (*Đang hợp tác*, *Tạm ngừng*).

6. **Lập phiếu nhập hàng (`ImportReceiptPage`)**:
   - Chọn Nhà cung cấp đang hoạt động.
   - Chọn sách cần nhập hàng và thêm vào phiếu nhập nháp.
   - Nhập **Số lượng nhập** và **Đơn giá nhập**; hệ thống tự tính thành tiền và tổng giá trị lô hàng.
   - Bấm **"Xác nhận nhập hàng"**: Hệ thống tự động tạo mã phiếu nhập, ghi nhận giao dịch và **cộng tăng số lượng tồn kho** tương ứng trong CSDL SQL Server theo cơ chế giao dịch an toàn (atomic).

7. **Lịch sử nhập hàng (`ImportHistoryPage`)**:
   - Tra cứu danh sách các phiếu nhập kho đã thực hiện trong quá khứ.
   - Xem lại chi tiết từng phiếu: Ngày giờ lập phiếu, người lập, nhà cung cấp, danh mục sách, đơn giá nhập gốc và tổng tiền thanh toán cho nhà cung cấp.

---

## 4. Cấu Trúc Mã Nguồn (Project Structure)

```text
QuanlyNhaSach/
│
├── QuanlyNhaSach.sln                 # File Solution chính
├── README.md                          # Tài liệu hướng dẫn sử dụng và kiểm thử
│
└── QuanlyNhaSach/                     # Thư mục Project WPF
    ├── App.xaml / App.xaml.cs         # Khởi chạy DI, Service container và AuthWindow
    ├── DBQuanLyNhaSach.sql            # Script SQL tạo CSDL và dữ liệu kiểm thử mẫu
    ├── banksettings.json              # Cấu hình tài khoản ngân hàng & VietQR
    ├── emailsettings.json             # Cấu hình SMTP Gmail gửi mã OTP
    │
    ├── Models/                        # Các Entity CSDL (EF Core DbContext)
    │   ├── QuanLyNhaSachContext.cs    # DbContext kết nối SQL Server
    │   ├── NGUOIDUNG.cs               # Thực thể Người dùng & Phân quyền
    │   ├── SACH.cs                    # Thực thể Sách & Tồn kho
    │   ├── DONHANG.cs / CHITIET...    # Thực thể Đơn hàng & Chi tiết
    │   ├── PHIEUNHAP.cs / CHITIET...  # Thực thể Phiếu nhập kho
    │   └── NHACUNGCAP.cs              # Thực thể Nhà cung cấp
    │
    ├── Services/                      # Các Service nghiệp vụ (Business Layer)
    │   ├── AuthService.cs             # Xử lý Đăng nhập, Đăng ký, BCrypt
    │   ├── BookService.cs             # Truy xuất và đồng bộ dữ liệu Sách
    │   ├── CartService.cs             # Nghiệp vụ Giỏ hàng của khách
    │   ├── OrderService.cs            # Quản lý Đơn hàng & POS bán lẻ
    │   ├── ImportService.cs           # Lập phiếu nhập & Lịch sử nhập hàng
    │   ├── NavigationService.cs       # Điều hướng giữa các cửa sổ & trang con
    │   ├── EmailService.cs            # Gửi email mã OTP xác thực
    │   └── InventoryService.cs        # Quản lý xuất/nhập/tồn kho
    │
    ├── ViewModels/                    # MVVM ViewModels
    │   ├── AuthViewModel.cs           # Logic Đăng nhập & Đăng ký
    │   ├── Customer/                  # ViewModels cho Khách hàng
    │   ├── Staff/                     # ViewModels cho Nhân viên
    │   └── Manager/                   # ViewModels cho Quản lý
    │
    └── Views/                         # Giao diện XAML
        ├── Auth/                      # Màn hình đăng nhập/đăng ký (AuthWindow)
        ├── Customer/                  # Cửa sổ & Các trang khách hàng
        ├── Staff/                     # Cửa sổ & Các trang nhân viên
        └── Manager/                   # Cửa sổ & Các trang quản trị
```

---

## 5. Xử Lý Sự Cố Thường Gặp (Troubleshooting)

1. **Lỗi không kết nối được Cơ sở dữ liệu (`Không thể kết nối đến cơ sở dữ liệu`)**:
   - Mở dịch vụ `Services.msc` trên Windows, kiểm tra dịch vụ `SQL Server (MSSQLSERVER)` hoặc `SQL Server (SQLEXPRESS)` đã ở trạng thái **Running** chưa.
   - Mở file `Models/QuanLyNhaSachContext.cs`, kiểm tra giá trị `CandidateConnectionStrings` và đổi lại tên máy chủ `Data Source` đúng với máy tính của bạn.
   - Đảm bảo đã chạy file `DBQuanLyNhaSach.sql` để tạo database `QuanlyNhaSach`.

2. **Không nhận được email mã xác thực khi đăng ký tài khoản mới**:
   - Kiểm tra file `emailsettings.json`. Đảm bảo đã bật *Xác minh 2 bước* trên tài khoản Google và sinh mã *Mật khẩu ứng dụng (App Password - 16 ký tự)* dán vào trường `AppPassword`.
   - Nếu không cần dùng email thực tế, có thể sử dụng ngay tài khoản khách hàng mẫu có sẵn `customer12` để kiểm thử.

3. **Font chữ hoặc hình ảnh không hiển thị**:
   - Dự án sử dụng hệ thống vector Icon Geometry XAML tích hợp sẵn trong `App.xaml` và bộ tài nguyên ảnh trong thư mục `Img/`.
   - Chạy lệnh `dotnet clean` và `dotnet build` để đảm bảo tài nguyên được biên dịch đầy đủ vào file nhị phân.
