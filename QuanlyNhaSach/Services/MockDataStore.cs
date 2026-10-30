using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using QuanlyNhaSach.Models;
using QuanlyNhaSach.Models.Enums;

namespace QuanlyNhaSach.Services
{
    /// <summary>
    /// Centralized application-scoped in-memory data store (Single Source of Truth)
    /// shared across Customer Portal, Staff Portal, and Manager/Admin Portal.
    /// </summary>
    public class MockDataStore
    {
        private static readonly Regex PhoneRegex = new(@"^\d{10}$", RegexOptions.Compiled);

        private int _nextBookId = 23;
        private int _nextUserId = 29;
        private int _nextSupplierId = 7;
        private int _nextOrderId = 1017;
        private int _nextImportReceiptId = 7;

        public ObservableCollection<Book> Books { get; } = new();
        public ObservableCollection<User> Users { get; } = new();
        public ObservableCollection<Supplier> Suppliers { get; } = new();
        public ObservableCollection<Order> Orders { get; } = new();
        public ObservableCollection<ImportReceipt> ImportReceipts { get; } = new();

        public event EventHandler? DataChanged;

        public MockDataStore()
        {
            SeedBooks();
            SeedUsers();
            SeedSuppliers();
            SeedImportReceipts();
            SeedOrders();
        }

        public int GenerateNextBookId() => _nextBookId++;
        public int GenerateNextUserId() => _nextUserId++;
        public int GenerateNextSupplierId() => _nextSupplierId++;
        public int GenerateNextOrderId() => _nextOrderId++;
        public int GenerateNextImportReceiptId() => _nextImportReceiptId++;
        public int PeekNextImportReceiptId() => _nextImportReceiptId;

        public void NotifyDataChanged()
        {
            DataChanged?.Invoke(this, EventArgs.Empty);
        }

        private void SeedBooks()
        {
            var initialBooks = new List<Book>
            {
                new Book
                {
                    MaSach = 1,
                    TenSach = "Dế Mèn Phiêu Lưu Ký",
                    TacGia = "Tô Hoài",
                    TheLoai = "Văn học thiếu nhi",
                    GiaBan = 85000m,
                    SoLuongTon = 12,
                    MauBia = "#1E3A8A",
                    AnhBia = "DeMenPhieuLuuKy",
                    MoTa = "Tác phẩm văn học thiếu nhi kinh điển của nhà văn Tô Hoài, kể về hành trình trưởng thành đầy ắp bài học nhân văn của chú Dế Mèn."
                },
                new Book
                {
                    MaSach = 2,
                    TenSach = "Tuổi Trẻ Đáng Giá Bao Nhiêu",
                    TacGia = "Rosie Nguyễn",
                    TheLoai = "Kỹ năng sống",
                    GiaBan = 99000m,
                    SoLuongTon = 8,
                    MauBia = "#0F766E",
                    AnhBia = "TuoiTreDangGiaBaoNhieu",
                    MoTa = "Cuốn sách truyền cảm hứng mạnh mẽ cho giới trẻ Việt Nam trên hành trình tự học, khám phá bản thân và theo đuổi ước mơ."
                },
                new Book
                {
                    MaSach = 3,
                    TenSach = "Nhà Giả Kim",
                    TacGia = "Paulo Coelho",
                    TheLoai = "Tiểu thuyết",
                    GiaBan = 79000m,
                    SoLuongTon = 15,
                    MauBia = "#B45309",
                    AnhBia = "NhaGiaKim",
                    MoTa = "Tiểu thuyết triết lý nổi tiếng toàn cầu về cậu bé chăn cừu Santiago và hành trình lắng nghe trái tim để đi tìm kho báu của đời mình."
                },
                new Book
                {
                    MaSach = 4,
                    TenSach = "Clean Code",
                    TacGia = "Robert C. Martin",
                    TheLoai = "Công nghệ thông tin",
                    GiaBan = 320000m,
                    SoLuongTon = 5,
                    MauBia = "#4338CA",
                    AnhBia = "CleanCode",
                    MoTa = "Cẩm nang kinh điển về nghệ thuật viết mã sạch, dễ đọc, dễ bảo trì dành cho mọi lập trình viên và kỹ sư phần mềm chuyên nghiệp."
                },
                new Book
                {
                    MaSach = 5,
                    TenSach = "C++ Primer",
                    TacGia = "Stanley B. Lippman et al.",
                    TheLoai = "Công nghệ thông tin",
                    GiaBan = 480000m,
                    SoLuongTon = 4,
                    MauBia = "#7C2D12",
                    AnhBia = "CppPrimer",
                    MoTa = "Tài liệu tham khảo toàn diện và sâu sắc về ngôn ngữ lập trình C++ hiện đại từ cơ bản đến nâng cao."
                },
                new Book
                {
                    MaSach = 6,
                    TenSach = "Đắc Nhân Tâm",
                    TacGia = "Dale Carnegie",
                    TheLoai = "Kỹ năng sống",
                    GiaBan = 88000m,
                    SoLuongTon = 35,
                    MauBia = "#1D4ED8",
                    AnhBia = "DacNhanTam",
                    MoTa = "Nghệ thuật thu phục lòng người và giao tiếp ứng xử kinh điển mọi thời đại."
                },
                new Book
                {
                    MaSach = 7,
                    TenSach = "Tư Duy Nhanh Và Chậm",
                    TacGia = "Daniel Kahneman",
                    TheLoai = "Tâm lý - Kinh tế",
                    GiaBan = 215000m,
                    SoLuongTon = 22,
                    MauBia = "#0369A1",
                    AnhBia = "TuDuyNhanhVaCham",
                    MoTa = "Công trình nghiên cứu đoạt giải Nobel Kinh tế giải mã hai hệ thống tư duy chi phối mọi quyết định của con người."
                },
                new Book
                {
                    MaSach = 8,
                    TenSach = "Sapiens: Lược Sử Loài Người",
                    TacGia = "Yuval Noah Harari",
                    TheLoai = "Lịch sử - Khoa học",
                    GiaBan = 245000m,
                    SoLuongTon = 18,
                    MauBia = "#92400E",
                    AnhBia = "Sapiens",
                    MoTa = "Bức tranh toàn cảnh về lịch sử tiến hóa và phát triển văn minh của loài người từ thời kỳ đồ đá đến thế kỷ 21."
                },
                new Book
                {
                    MaSach = 9,
                    TenSach = "Mắt Biếc",
                    TacGia = "Nguyễn Nhật Ánh",
                    TheLoai = "Văn học Việt Nam",
                    GiaBan = 110000m,
                    SoLuongTon = 28,
                    MauBia = "#0F766E",
                    AnhBia = "MatBiec",
                    MoTa = "Câu chuyện tình đơn phương trong trẻo và day dứt của Ngạn dành cho Hà Lan nơi làng Đo Đo bình yên."
                },
                new Book
                {
                    MaSach = 10,
                    TenSach = "Cho Tôi Xin Một Vé Đi Tuổi Thơ",
                    TacGia = "Nguyễn Nhật Ánh",
                    TheLoai = "Văn học thiếu nhi",
                    GiaBan = 95000m,
                    SoLuongTon = 14,
                    MauBia = "#D97706",
                    AnhBia = "ChoToiXinMotVeDiTuoiTho",
                    MoTa = "Chuyến tàu trở về thế giới tuổi thơ hồn nhiên, dí dỏm và đầy triết lý sâu lắng của nhà văn Nguyễn Nhật Ánh."
                },
                new Book
                {
                    MaSach = 11,
                    TenSach = "Số Đỏ",
                    TacGia = "Vũ Trọng Phụng",
                    TheLoai = "Văn học kinh điển",
                    GiaBan = 75000m,
                    SoLuongTon = 6,
                    MauBia = "#991B1B",
                    AnhBia = "SoDo",
                    MoTa = "Kiệt tác trào phúng xuất sắc của văn học hiện thực phê phán Việt Nam qua nhân vật Xuân Tóc Đỏ."
                },
                new Book
                {
                    MaSach = 12,
                    TenSach = "Chí Phèo",
                    TacGia = "Nam Cao",
                    TheLoai = "Văn học kinh điển",
                    GiaBan = 68000m,
                    SoLuongTon = 19,
                    MauBia = "#374151",
                    AnhBia = "ChiPheo",
                    MoTa = "Tuyển tập truyện ngắn hiện thực sâu sắc của nhà văn Nam Cao về bi kịch người nông dân và trí thức nghèo trước Cách mạng."
                },
                new Book
                {
                    MaSach = 13,
                    TenSach = "Thiết Kế Hướng Miền (Domain-Driven Design)",
                    TacGia = "Eric Evans",
                    TheLoai = "Công nghệ thông tin",
                    GiaBan = 390000m,
                    SoLuongTon = 0,
                    MauBia = "#1E3A8A",
                    AnhBia = "DomainDrivenDesign",
                    MoTa = "Phương pháp luận thiết kế phần mềm hướng miền nghiệp vụ giải quyết độ phức tạp cốt lõi trong các hệ thống lớn."
                },
                new Book
                {
                    MaSach = 14,
                    TenSach = "The Pragmatic Programmer",
                    TacGia = "Andrew Hunt & David Thomas",
                    TheLoai = "Công nghệ thông tin",
                    GiaBan = 350000m,
                    SoLuongTon = 7,
                    MauBia = "#312E81",
                    AnhBia = "PragmaticProgrammer",
                    MoTa = "Hành trình từ lập trình viên thực thụ đến bậc thầy kiến tạo phần mềm thực용 và bền vững."
                },
                new Book
                {
                    MaSach = 15,
                    TenSach = "Design Patterns: Elements of Reusable OO Software",
                    TacGia = "Erich Gamma et al.",
                    TheLoai = "Công nghệ thông tin",
                    GiaBan = 410000m,
                    SoLuongTon = 11,
                    MauBia = "#065F46",
                    AnhBia = "DesignPatterns",
                    MoTa = "Cuốn sách kinh điển của nhóm Gang of Four về 23 mẫu thiết kế hướng đối tượng nền tảng."
                },
                new Book
                {
                    MaSach = 16,
                    TenSach = "Cấu Trúc Dữ Liệu Và Giải Thuật",
                    TacGia = "Lê Minh Hoàng",
                    TheLoai = "Giáo trình đại học",
                    GiaBan = 165000m,
                    SoLuongTon = 25,
                    MauBia = "#1E40AF",
                    AnhBia = "CTDLVaGiaiThuat",
                    MoTa = "Giáo trình toàn diện về bài toán quy hoạch động, đồ thị, đệ quy và cấu trúc dữ liệu dành cho sinh viên CNTT."
                },
                new Book
                {
                    MaSach = 17,
                    TenSach = "Cây Cam Ngọt Của Tôi",
                    TacGia = "José Mauro de Vasconcelos",
                    TheLoai = "Tiểu thuyết",
                    GiaBan = 108000m,
                    SoLuongTon = 3,
                    MauBia = "#C2410C",
                    AnhBia = "CayCamNgotCuaToi",
                    MoTa = "Câu chuyện cảm động về cậu bé Zezé năm tuổi giàu trí tưởng tượng và tấm lòng nhân hậu."
                },
                new Book
                {
                    MaSach = 18,
                    TenSach = "Muôn Kiếp Nhân Sinh",
                    TacGia = "Nguyên Phong",
                    TheLoai = "Văn hóa - Tâm linh",
                    GiaBan = 168000m,
                    SoLuongTon = 30,
                    MauBia = "#6D28D9",
                    AnhBia = "MuonKiepNhanSinh",
                    MoTa = "Những câu chuyện tiền kiếp kỳ lạ và bài học sâu sắc về luật Nhân Quả trong vũ trụ."
                },
                new Book
                {
                    MaSach = 19,
                    TenSach = "Hành Trình Về Phương Đông",
                    TacGia = "Baird T. Spalding",
                    TheLoai = "Văn hóa - Tâm linh",
                    GiaBan = 86000m,
                    SoLuongTon = 16,
                    MauBia = "#B45309",
                    AnhBia = "HanhTrinhVePhuongDong",
                    MoTa = "Chuyến khảo cứu văn hóa và triết học phương Đông sâu sắc của đoàn khoa học gia Hoàng gia."
                },
                new Book
                {
                    MaSach = 20,
                    TenSach = "Lập Trình C# & .NET Toàn Tập",
                    TacGia = "Phạm Huy Hoàng",
                    TheLoai = "Công nghệ thông tin",
                    GiaBan = 275000m,
                    SoLuongTon = 0,
                    MauBia = "#4F46E5",
                    AnhBia = "LapTrinhCSharp",
                    MoTa = "Tài liệu thực hành phát triển ứng dụng Desktop WPF, Web API và kiến trúc MVVM với .NET hiện đại."
                },
                new Book
                {
                    MaSach = 21,
                    TenSach = "Nghệ Thuật Tinh Tế Của Việc Đếch Quan Tâm",
                    TacGia = "Mark Manson",
                    TheLoai = "Kỹ năng sống",
                    GiaBan = 115000m,
                    SoLuongTon = 9,
                    MauBia = "#EA580C",
                    AnhBia = "NgheThuatTinhTe",
                    MoTa = "Góc nhìn thẳng thắn giúp bạn tập trung vào những giá trị thực sự quan trọng trong cuộc sống."
                },
                new Book
                {
                    MaSach = 22,
                    TenSach = "Tâm Lý Học Tội Phạm",
                    TacGia = "Stanton E. Samenow",
                    TheLoai = "Tâm lý học",
                    GiaBan = 149000m,
                    SoLuongTon = 20,
                    MauBia = "#1F2937",
                    AnhBia = "TamLyHocToiPham",
                    MoTa = "Phân tích chuyên sâu về cơ chế tâm lý và suy nghĩ bên trong hành vi phạm tội."
                }
            };

            foreach (var book in initialBooks)
            {
                Books.Add(book);
            }
        }

        private void SeedUsers()
        {
            var initialUsers = new List<User>
            {
                // 1. Primary Demo Customer (#1)
                new Customer
                {
                    MaND = 1,
                    Username = "customer@example.com",
                    Password = "Customer123!",
                    HoTen = "Nguyễn Văn An",
                    SDT = "0901234567",
                    DiaChi = "280 An Dương Vương, Phường 4, Quận 5, TP.HCM",
                    Role = UserRole.Customer,
                    IsActive = true
                },
                // 2. Primary Demo Staff (#2)
                new Staff
                {
                    MaND = 2,
                    Username = "staff@example.com",
                    Password = "Staff123!",
                    HoTen = "Trần Thị Minh Thư",
                    SDT = "0912345678",
                    DiaChi = "Nhà sách Nhóm 1 — Chi nhánh Quận 5, TP.HCM",
                    Role = UserRole.Staff,
                    IsActive = true
                },
                // 3. Secondary Demo Customer (#3)
                new Customer
                {
                    MaND = 3,
                    Username = "lehoangnam@example.com",
                    Password = "Customer123!",
                    HoTen = "Lê Hoàng Nam",
                    SDT = "0987654321",
                    DiaChi = "123 Nguyễn Văn Cừ, Phường 2, Quận 5, TP.HCM",
                    Role = UserRole.Customer,
                    IsActive = true
                },
                // 4. Locked Demo Customer (#4)
                new Customer
                {
                    MaND = 4,
                    Username = "locked@example.com",
                    Password = "Locked123!",
                    HoTen = "Phạm Quốc Bảo",
                    SDT = "0933445566",
                    DiaChi = "45 Lê Lợi, Phường Bến Nghé, Quận 1, TP.HCM",
                    Role = UserRole.Customer,
                    IsActive = false
                },
                // 5. Primary Demo Manager (#5)
                new User
                {
                    MaND = 5,
                    Username = "manager@example.com",
                    Password = "Manager123!",
                    HoTen = "Lê Quản Trị",
                    SDT = "0909888999",
                    DiaChi = "Trụ sở Nhà sách Nhóm 1 — 280 An Dương Vương, Quận 5, TP.HCM",
                    Role = UserRole.Manager,
                    IsActive = true
                },

                // Additional Staff Accounts (#6 .. #15 => Total 11 Staff)
                new Staff
                {
                    MaND = 6,
                    Username = "nv.hoangtuan@example.com",
                    Password = "Staff123!",
                    HoTen = "Hoàng Anh Tuấn",
                    SDT = "0913111222",
                    DiaChi = "12 Trần Hưng Đạo, Quận 1, TP.HCM",
                    IsActive = true
                },
                new Staff
                {
                    MaND = 7,
                    Username = "nv.lananh@example.com",
                    Password = "Staff123!",
                    HoTen = "Đặng Thị Lan Anh",
                    SDT = "0914222333",
                    DiaChi = "89 Nguyễn Thị Minh Khai, Quận 3, TP.HCM",
                    IsActive = true
                },
                new Staff
                {
                    MaND = 8,
                    Username = "nv.minhkhoi@example.com",
                    Password = "Staff123!",
                    HoTen = "Vũ Minh Khôi",
                    SDT = "0915333444",
                    DiaChi = "210 Lý Thường Kiệt, Quận 10, TP.HCM",
                    IsActive = true
                },
                new Staff
                {
                    MaND = 9,
                    Username = "nv.thanhha@example.com",
                    Password = "Staff123!",
                    HoTen = "Bùi Thanh Hà",
                    SDT = "0916444555",
                    DiaChi = "56 Võ Văn Tần, Quận 3, TP.HCM",
                    IsActive = true
                },
                new Staff
                {
                    MaND = 10,
                    Username = "nv.quochuy@example.com",
                    Password = "Staff123!",
                    HoTen = "Ngô Quốc Huy",
                    SDT = "0917555666",
                    DiaChi = "304 Ba Tháng Hai, Quận 10, TP.HCM",
                    IsActive = false
                },
                new Staff
                {
                    MaND = 11,
                    Username = "nv.thuytrang@example.com",
                    Password = "Staff123!",
                    HoTen = "Phan Thùy Trang",
                    SDT = "0918666777",
                    DiaChi = "15 Nguyễn Trãi, Quận 5, TP.HCM",
                    IsActive = true
                },
                new Staff
                {
                    MaND = 12,
                    Username = "nv.ducmanh@example.com",
                    Password = "Staff123!",
                    HoTen = "Trịnh Đức Mạnh",
                    SDT = "0919777888",
                    DiaChi = "77 Hùng Vương, Quận 5, TP.HCM",
                    IsActive = true
                },
                new Staff
                {
                    MaND = 13,
                    Username = "nv.kimngan@example.com",
                    Password = "Staff123!",
                    HoTen = "Đỗ Kim Ngân",
                    SDT = "0921888999",
                    DiaChi = "142 Điện Biên Phủ, Quận Bình Thạnh, TP.HCM",
                    IsActive = true
                },
                new Staff
                {
                    MaND = 14,
                    Username = "nv.giabao@example.com",
                    Password = "Staff123!",
                    HoTen = "Lý Gia Bảo",
                    SDT = "0922999000",
                    DiaChi = "90 Cách Mạng Tháng Tám, Quận 3, TP.HCM",
                    IsActive = false
                },
                new Staff
                {
                    MaND = 15,
                    Username = "nv.phuongthao@example.com",
                    Password = "Staff123!",
                    HoTen = "Mai Phương Thảo",
                    SDT = "0923123123",
                    DiaChi = "18 Lê Văn Sỹ, Quận Phú Nhuận, TP.HCM",
                    IsActive = true
                },

                // Additional Customer Accounts (#16 .. #28 => Total 16 Customers)
                new Customer
                {
                    MaND = 16,
                    Username = "kh.minhchau@example.com",
                    Password = "Customer123!",
                    HoTen = "Trần Minh Châu",
                    SDT = "0931112233",
                    DiaChi = "19 Nguyễn Đình Chiểu, Quận 1, TP.HCM",
                    IsActive = true
                },
                new Customer
                {
                    MaND = 17,
                    Username = "kh.hoanglong@example.com",
                    Password = "Customer123!",
                    HoTen = "Phạm Hoàng Long",
                    SDT = "0932223344",
                    DiaChi = "84 Lê Đại Hành, Quận 11, TP.HCM",
                    IsActive = true
                },
                new Customer
                {
                    MaND = 18,
                    Username = "kh.baongoc@example.com",
                    Password = "Customer123!",
                    HoTen = "Võ Bảo Ngọc",
                    SDT = "0933334455",
                    DiaChi = "205 Hai Bà Trưng, Quận 3, TP.HCM",
                    IsActive = true
                },
                new Customer
                {
                    MaND = 19,
                    Username = "kh.anhkhoa@example.com",
                    Password = "Customer123!",
                    HoTen = "Đinh Anh Khoa",
                    SDT = "0934445566",
                    DiaChi = "62 Phan Xích Long, Quận Phú Nhuận, TP.HCM",
                    IsActive = true
                },
                new Customer
                {
                    MaND = 20,
                    Username = "kh.thanhmai@example.com",
                    Password = "Customer123!",
                    HoTen = "Lương Thanh Mai",
                    SDT = "0935556677",
                    DiaChi = "108 Nguyễn Văn Đậu, Quận Bình Thạnh, TP.HCM",
                    IsActive = false
                },
                new Customer
                {
                    MaND = 21,
                    Username = "kh.quangvinh@example.com",
                    Password = "Customer123!",
                    HoTen = "Hồ Quang Vinh",
                    SDT = "0936667788",
                    DiaChi = "33 Hoàng Diệu, Quận 4, TP.HCM",
                    IsActive = true
                },
                new Customer
                {
                    MaND = 22,
                    Username = "kh.tuankiet@example.com",
                    Password = "Customer123!",
                    HoTen = "Dương Tuấn Kiệt",
                    SDT = "0937778899",
                    DiaChi = "150 Tô Hiến Thành, Quận 10, TP.HCM",
                    IsActive = true
                },
                new Customer
                {
                    MaND = 23,
                    Username = "kh.nhuquynh@example.com",
                    Password = "Customer123!",
                    HoTen = "Cao Như Quỳnh",
                    SDT = "0938889900",
                    DiaChi = "47 Nguyễn Thị Thập, Quận 7, TP.HCM",
                    IsActive = true
                },
                new Customer
                {
                    MaND = 24,
                    Username = "kh.vanhung@example.com",
                    Password = "Customer123!",
                    HoTen = "Tạ Văn Hùng",
                    SDT = "0939990011",
                    DiaChi = "95 Quang Trung, Quận Gò Vấp, TP.HCM",
                    IsActive = false
                },
                new Customer
                {
                    MaND = 25,
                    Username = "kh.khanhlinh@example.com",
                    Password = "Customer123!",
                    HoTen = "Bạch Khánh Linh",
                    SDT = "0941122334",
                    DiaChi = "12 Cộng Hòa, Quận Tân Bình, TP.HCM",
                    IsActive = true
                },
                new Customer
                {
                    MaND = 26,
                    Username = "kh.thanhdat@example.com",
                    Password = "Customer123!",
                    HoTen = "Đoàn Thành Đạt",
                    SDT = "0942233445",
                    DiaChi = "72 Võ Văn Ngân, TP. Thủ Đức, TP.HCM",
                    IsActive = true
                },
                new Customer
                {
                    MaND = 27,
                    Username = "kh.myduyen@example.com",
                    Password = "Customer123!",
                    HoTen = "Lâm Mỹ Duyên",
                    SDT = "0943344556",
                    DiaChi = "128 Kinh Dương Vương, Quận 6, TP.HCM",
                    IsActive = true
                },
                new Customer
                {
                    MaND = 28,
                    Username = "kh.minhtriet@example.com",
                    Password = "Customer123!",
                    HoTen = "Huỳnh Minh Triết",
                    SDT = "0944455667",
                    DiaChi = "54 Âu Cơ, Quận Tân Phú, TP.HCM",
                    IsActive = true
                }
            };

            foreach (var user in initialUsers)
            {
                Users.Add(user);
            }
        }

        private void SeedSuppliers()
        {
            var initialSuppliers = new List<Supplier>
            {
                new Supplier
                {
                    MaNCC = 1,
                    TenNCC = "Công ty CP Phát hành Sách TP.HCM (FAHASA)",
                    SDT = "0283822579",
                    Email = "contact@fahasa.com.vn",
                    DiaChi = "60-62 Lê Lợi, Phường Bến Nghé, Quận 1, TP.HCM",
                    IsActive = true
                },
                new Supplier
                {
                    MaNCC = 2,
                    TenNCC = "Nhà xuất bản Trẻ (NXB Trẻ)",
                    SDT = "0283931628",
                    Email = "hopthubandoc@nxbtre.com.vn",
                    DiaChi = "161B Lý Chính Thắng, Phường Võ Thị Sáu, Quận 3, TP.HCM",
                    IsActive = true
                },
                new Supplier
                {
                    MaNCC = 3,
                    TenNCC = "Công ty Văn hóa & Truyền thông Nhã Nam",
                    SDT = "0283847985",
                    Email = "info@nhanam.vn",
                    DiaChi = "015 Lô B Chung cư 43 Hồ Văn Huê, Quận Phú Nhuận, TP.HCM",
                    IsActive = true
                },
                new Supplier
                {
                    MaNCC = 4,
                    TenNCC = "Nhà xuất bản Kim Đồng",
                    SDT = "0283939046",
                    Email = "cskh@nxbkimdong.com.vn",
                    DiaChi = "248 Cống Quỳnh, Phường Phạm Ngũ Lão, Quận 1, TP.HCM",
                    IsActive = true
                },
                new Supplier
                {
                    MaNCC = 5,
                    TenNCC = "Công ty CP Sách Alpha (Alpha Books)",
                    SDT = "0283820999",
                    Email = "kinhdoanh@alphabooks.vn",
                    DiaChi = "138C Nguyễn Đình Chiểu, Phường 6, Quận 3, TP.HCM",
                    IsActive = true
                },
                new Supplier
                {
                    MaNCC = 6,
                    TenNCC = "Nhà xuất bản Tổng hợp TP.HCM",
                    SDT = "0283825680",
                    Email = "tonghop@nxbhcm.com.vn",
                    DiaChi = "62 Nguyễn Thị Minh Khai, Phường Đa Kao, Quận 1, TP.HCM",
                    IsActive = true
                }
            };

            foreach (var supplier in initialSuppliers)
            {
                Suppliers.Add(supplier);
            }
        }

        private void SeedImportReceipts()
        {
            var now = DateTime.Now;

            var initialReceipts = new List<ImportReceipt>
            {
                CreateSeededReceipt(
                    maPN: 1,
                    supplier: Suppliers[3], // NXB Kim Đồng
                    ngayLap: now.AddDays(-18),
                    ghiChu: "Nhập định kỳ sách văn học thiếu nhi đầu tháng",
                    details: new List<ImportReceiptDetail>
                    {
                        new ImportReceiptDetail { MaSach = 1, TenSach = "Dế Mèn Phiêu Lưu Ký", TacGia = "Tô Hoài", CurrentStock = 12, SoLuongNhap = 20, DonGiaNhap = 55000m },
                        new ImportReceiptDetail { MaSach = 10, TenSach = "Cho Tôi Xin Một Vé Đi Tuổi Thơ", TacGia = "Nguyễn Nhật Ánh", CurrentStock = 14, SoLuongNhap = 15, DonGiaNhap = 62000m },
                        new ImportReceiptDetail { MaSach = 17, TenSach = "Cây Cam Ngọt Của Tôi", TacGia = "José Mauro de Vasconcelos", CurrentStock = 3, SoLuongNhap = 10, DonGiaNhap = 70000m }
                    }),
                CreateSeededReceipt(
                    maPN: 2,
                    supplier: Suppliers[1], // NXB Trẻ
                    ngayLap: now.AddDays(-14),
                    ghiChu: "Nhập bổ sung sách kỹ năng và tiểu thuyết",
                    details: new List<ImportReceiptDetail>
                    {
                        new ImportReceiptDetail { MaSach = 2, TenSach = "Tuổi Trẻ Đáng Giá Bao Nhiêu", TacGia = "Rosie Nguyễn", CurrentStock = 8, SoLuongNhap = 18, DonGiaNhap = 65000m },
                        new ImportReceiptDetail { MaSach = 9, TenSach = "Mắt Biếc", TacGia = "Nguyễn Nhật Ánh", CurrentStock = 28, SoLuongNhap = 25, DonGiaNhap = 72000m },
                        new ImportReceiptDetail { MaSach = 6, TenSach = "Đắc Nhân Tâm", TacGia = "Dale Carnegie", CurrentStock = 35, SoLuongNhap = 30, DonGiaNhap = 58000m }
                    }),
                CreateSeededReceipt(
                    maPN: 3,
                    supplier: Suppliers[2], // Nhã Nam
                    ngayLap: now.AddDays(-10),
                    ghiChu: "Nhập sách triết lý & lịch sử bán chạy",
                    details: new List<ImportReceiptDetail>
                    {
                        new ImportReceiptDetail { MaSach = 3, TenSach = "Nhà Giả Kim", TacGia = "Paulo Coelho", CurrentStock = 15, SoLuongNhap = 22, DonGiaNhap = 52000m },
                        new ImportReceiptDetail { MaSach = 8, TenSach = "Sapiens: Lược Sử Loài Người", TacGia = "Yuval Noah Harari", CurrentStock = 18, SoLuongNhap = 15, DonGiaNhap = 165000m },
                        new ImportReceiptDetail { MaSach = 11, TenSach = "Số Đỏ", TacGia = "Vũ Trọng Phụng", CurrentStock = 6, SoLuongNhap = 12, DonGiaNhap = 48000m }
                    }),
                CreateSeededReceipt(
                    maPN: 4,
                    supplier: Suppliers[4], // Alpha Books
                    ngayLap: now.AddDays(-7),
                    ghiChu: "Nhập sách chuyên ngành Công nghệ thông tin & Tâm lý",
                    details: new List<ImportReceiptDetail>
                    {
                        new ImportReceiptDetail { MaSach = 4, TenSach = "Clean Code", TacGia = "Robert C. Martin", CurrentStock = 5, SoLuongNhap = 10, DonGiaNhap = 220000m },
                        new ImportReceiptDetail { MaSach = 5, TenSach = "C++ Primer", TacGia = "Stanley B. Lippman et al.", CurrentStock = 4, SoLuongNhap = 8, DonGiaNhap = 340000m },
                        new ImportReceiptDetail { MaSach = 7, TenSach = "Tư Duy Nhanh Và Chậm", TacGia = "Daniel Kahneman", CurrentStock = 22, SoLuongNhap = 16, DonGiaNhap = 145000m }
                    }),
                CreateSeededReceipt(
                    maPN: 5,
                    supplier: Suppliers[0], // FAHASA
                    ngayLap: now.AddDays(-4),
                    ghiChu: "Nhập sách kỹ thuật phần mềm & văn hóa",
                    details: new List<ImportReceiptDetail>
                    {
                        new ImportReceiptDetail { MaSach = 14, TenSach = "The Pragmatic Programmer", TacGia = "Andrew Hunt & David Thomas", CurrentStock = 7, SoLuongNhap = 10, DonGiaNhap = 240000m },
                        new ImportReceiptDetail { MaSach = 15, TenSach = "Design Patterns: Elements of Reusable OO Software", TacGia = "Erich Gamma et al.", CurrentStock = 11, SoLuongNhap = 10, DonGiaNhap = 285000m },
                        new ImportReceiptDetail { MaSach = 18, TenSach = "Muôn Kiếp Nhân Sinh", TacGia = "Nguyên Phong", CurrentStock = 30, SoLuongNhap = 20, DonGiaNhap = 112000m }
                    }),
                CreateSeededReceipt(
                    maPN: 6,
                    supplier: Suppliers[5], // NXB Tổng hợp TP.HCM
                    ngayLap: now.AddDays(-2),
                    ghiChu: "Nhập giáo trình đại học và sách tâm lý học",
                    details: new List<ImportReceiptDetail>
                    {
                        new ImportReceiptDetail { MaSach = 16, TenSach = "Cấu Trúc Dữ Liệu Và Giải Thuật", TacGia = "Lê Minh Hoàng", CurrentStock = 25, SoLuongNhap = 20, DonGiaNhap = 110000m },
                        new ImportReceiptDetail { MaSach = 19, TenSach = "Hành Trình Về Phương Đông", TacGia = "Baird T. Spalding", CurrentStock = 16, SoLuongNhap = 15, DonGiaNhap = 56000m },
                        new ImportReceiptDetail { MaSach = 22, TenSach = "Tâm Lý Học Tội Phạm", TacGia = "Stanton E. Samenow", CurrentStock = 20, SoLuongNhap = 15, DonGiaNhap = 98000m }
                    })
            };

            foreach (var receipt in initialReceipts)
            {
                ImportReceipts.Add(receipt);
            }
        }

        private static ImportReceipt CreateSeededReceipt(
            int maPN,
            Supplier supplier,
            DateTime ngayLap,
            string ghiChu,
            List<ImportReceiptDetail> details)
        {
            var receipt = new ImportReceipt
            {
                MaPN = maPN,
                MaNCC = supplier.MaNCC,
                TenNCC = supplier.TenNCC,
                SdtNCC = supplier.SDT,
                EmailNCC = supplier.Email,
                DiaChiNCC = supplier.DiaChi,
                NgayLap = ngayLap,
                TrangThai = ImportReceiptStatus.Confirmed,
                NguoiLapMaND = 5,
                NguoiLapHoTen = "Lê Quản Trị",
                GhiChu = ghiChu,
                TongTien = details.Sum(d => d.ThanhTien)
            };

            foreach (var d in details)
            {
                receipt.Details.Add(d);
            }

            return receipt;
        }

        private void SeedOrders()
        {
            var now = DateTime.Now;

            var initialOrders = new List<Order>
            {
                // #1001: New order, BankTransfer, Pending payment, NotIssued, sufficient stock
                new Order
                {
                    MaDH = 1001,
                    MaND = 1,
                    HoTenKhachHang = "Nguyễn Văn An",
                    SDTKhachHang = "0901234567",
                    DiaChiGiaoHang = "280 An Dương Vương, Phường 4, Quận 5, TP.HCM",
                    NgayDat = now.AddMinutes(-35),
                    NgayCapNhat = now.AddMinutes(-35),
                    TongTien = 249000m,
                    Method = PaymentMethod.BankTransfer,
                    PaymentStatus = PaymentStatus.Pending,
                    Status = OrderStatus.New,
                    StockOutStatus = StockOutStatus.NotIssued,
                    Items = new List<OrderItem>
                    {
                        new OrderItem
                        {
                            MaSach = 1,
                            TenSach = "Dế Mèn Phiêu Lưu Ký",
                            TacGia = "Tô Hoài",
                            DonGia = 85000m,
                            SoLuongMua = 2
                        },
                        new OrderItem
                        {
                            MaSach = 3,
                            TenSach = "Nhà Giả Kim",
                            TacGia = "Paulo Coelho",
                            DonGia = 79000m,
                            SoLuongMua = 1
                        }
                    }
                },

                // #1002: Confirmed order awaiting Stock-Out & Cash payment, sufficient stock
                new Order
                {
                    MaDH = 1002,
                    MaND = 3,
                    HoTenKhachHang = "Lê Hoàng Nam",
                    SDTKhachHang = "0987654321",
                    DiaChiGiaoHang = "123 Nguyễn Văn Cừ, Phường 2, Quận 5, TP.HCM",
                    NgayDat = now.AddHours(-2),
                    NgayCapNhat = now.AddHours(-1).AddMinutes(-20),
                    TongTien = 518000m,
                    Method = PaymentMethod.Cash,
                    PaymentStatus = PaymentStatus.Pending,
                    Status = OrderStatus.Confirmed,
                    StockOutStatus = StockOutStatus.NotIssued,
                    Items = new List<OrderItem>
                    {
                        new OrderItem
                        {
                            MaSach = 2,
                            TenSach = "Tuổi Trẻ Đáng Giá Bao Nhiêu",
                            TacGia = "Rosie Nguyễn",
                            DonGia = 99000m,
                            SoLuongMua = 2
                        },
                        new OrderItem
                        {
                            MaSach = 4,
                            TenSach = "Clean Code",
                            TacGia = "Robert C. Martin",
                            DonGia = 320000m,
                            SoLuongMua = 1
                        }
                    }
                },

                // #1003: Confirmed order with INSUFFICIENT STOCK (C++ Primer ordered 6, stock is 4)
                new Order
                {
                    MaDH = 1003,
                    MaND = 1,
                    HoTenKhachHang = "Nguyễn Văn An",
                    SDTKhachHang = "0901234567",
                    DiaChiGiaoHang = "280 An Dương Vương, Phường 4, Quận 5, TP.HCM",
                    NgayDat = now.AddHours(-4),
                    NgayCapNhat = now.AddHours(-3),
                    TongTien = 3200000m,
                    Method = PaymentMethod.COD,
                    PaymentStatus = PaymentStatus.PayOnDelivery,
                    Status = OrderStatus.Confirmed,
                    StockOutStatus = StockOutStatus.InsufficientStock,
                    Items = new List<OrderItem>
                    {
                        new OrderItem
                        {
                            MaSach = 5,
                            TenSach = "C++ Primer",
                            TacGia = "Stanley B. Lippman et al.",
                            DonGia = 480000m,
                            SoLuongMua = 6
                        },
                        new OrderItem
                        {
                            MaSach = 4,
                            TenSach = "Clean Code",
                            TacGia = "Robert C. Martin",
                            DonGia = 320000m,
                            SoLuongMua = 1
                        }
                    }
                },

                // #1004: Shipping order, COD payment awaiting collection on delivery, Stock already issued
                new Order
                {
                    MaDH = 1004,
                    MaND = 3,
                    HoTenKhachHang = "Lê Hoàng Nam",
                    SDTKhachHang = "0987654321",
                    DiaChiGiaoHang = "123 Nguyễn Văn Cừ, Phường 2, Quận 5, TP.HCM",
                    NgayDat = now.AddHours(-8),
                    NgayCapNhat = now.AddHours(-2),
                    TongTien = 158000m,
                    Method = PaymentMethod.COD,
                    PaymentStatus = PaymentStatus.PayOnDelivery,
                    Status = OrderStatus.Shipping,
                    StockOutStatus = StockOutStatus.Issued,
                    StockOutDate = now.AddHours(-3),
                    Items = new List<OrderItem>
                    {
                        new OrderItem
                        {
                            MaSach = 3,
                            TenSach = "Nhà Giả Kim",
                            TacGia = "Paulo Coelho",
                            DonGia = 79000m,
                            SoLuongMua = 2
                        }
                    }
                },

                // #1005: Completed order, BankTransfer Paid, Stock Issued (Day -1)
                new Order
                {
                    MaDH = 1005,
                    MaND = 1,
                    HoTenKhachHang = "Nguyễn Văn An",
                    SDTKhachHang = "0901234567",
                    DiaChiGiaoHang = "280 An Dương Vương, Phường 4, Quận 5, TP.HCM",
                    NgayDat = now.AddDays(-1),
                    NgayCapNhat = now.AddHours(-6),
                    TongTien = 405000m,
                    PaidAmount = 405000m,
                    Method = PaymentMethod.BankTransfer,
                    PaymentStatus = PaymentStatus.Paid,
                    PaymentNote = "[Mô phỏng]: Đã xác nhận chuyển khoản nội bộ",
                    Status = OrderStatus.Completed,
                    StockOutStatus = StockOutStatus.Issued,
                    StockOutDate = now.AddHours(-12),
                    Items = new List<OrderItem>
                    {
                        new OrderItem
                        {
                            MaSach = 4,
                            TenSach = "Clean Code",
                            TacGia = "Robert C. Martin",
                            DonGia = 320000m,
                            SoLuongMua = 1
                        },
                        new OrderItem
                        {
                            MaSach = 1,
                            TenSach = "Dế Mèn Phiêu Lưu Ký",
                            TacGia = "Tô Hoài",
                            DonGia = 85000m,
                            SoLuongMua = 1
                        }
                    }
                },

                // #1006: Cancelled order
                new Order
                {
                    MaDH = 1006,
                    MaND = 4,
                    HoTenKhachHang = "Phạm Quốc Bảo",
                    SDTKhachHang = "0933445566",
                    DiaChiGiaoHang = "45 Lê Lợi, Phường Bến Nghé, Quận 1, TP.HCM",
                    NgayDat = now.AddDays(-2),
                    NgayCapNhat = now.AddDays(-1),
                    TongTien = 99000m,
                    Method = PaymentMethod.Cash,
                    PaymentStatus = PaymentStatus.Pending,
                    Status = OrderStatus.Cancelled,
                    StockOutStatus = StockOutStatus.NotIssued,
                    Items = new List<OrderItem>
                    {
                        new OrderItem
                        {
                            MaSach = 2,
                            TenSach = "Tuổi Trẻ Đáng Giá Bao Nhiêu",
                            TacGia = "Rosie Nguyễn",
                            DonGia = 99000m,
                            SoLuongMua = 1
                        }
                    }
                },

                // #1007: Completed today, Cash Paid
                new Order
                {
                    MaDH = 1007,
                    MaND = 16,
                    HoTenKhachHang = "Trần Minh Châu",
                    SDTKhachHang = "0931112233",
                    DiaChiGiaoHang = "19 Nguyễn Đình Chiểu, Quận 1, TP.HCM",
                    NgayDat = now.Date.AddHours(9).AddMinutes(15),
                    NgayCapNhat = now.Date.AddHours(10).AddMinutes(30),
                    TongTien = 594000m,
                    PaidAmount = 600000m,
                    Method = PaymentMethod.Cash,
                    PaymentStatus = PaymentStatus.Paid,
                    PaymentNote = "Thu tiền mặt: 600,000 ₫ (Tiền thừa trả khách: 6,000 ₫)",
                    Status = OrderStatus.Completed,
                    StockOutStatus = StockOutStatus.Issued,
                    StockOutDate = now.Date.AddHours(9).AddMinutes(45),
                    Items = new List<OrderItem>
                    {
                        new OrderItem { MaSach = 6, TenSach = "Đắc Nhân Tâm", TacGia = "Dale Carnegie", DonGia = 88000m, SoLuongMua = 3 },
                        new OrderItem { MaSach = 9, TenSach = "Mắt Biếc", TacGia = "Nguyễn Nhật Ánh", DonGia = 110000m, SoLuongMua = 3 }
                    }
                },

                // #1008: Completed today, BankTransfer Paid
                new Order
                {
                    MaDH = 1008,
                    MaND = 17,
                    HoTenKhachHang = "Phạm Hoàng Long",
                    SDTKhachHang = "0932223344",
                    DiaChiGiaoHang = "84 Lê Đại Hành, Quận 11, TP.HCM",
                    NgayDat = now.Date.AddHours(11),
                    NgayCapNhat = now.Date.AddHours(13),
                    TongTien = 665000m,
                    PaidAmount = 665000m,
                    Method = PaymentMethod.BankTransfer,
                    PaymentStatus = PaymentStatus.Paid,
                    PaymentNote = "[Mô phỏng]: Đã xác nhận chuyển khoản 665,000 ₫",
                    Status = OrderStatus.Completed,
                    StockOutStatus = StockOutStatus.Issued,
                    StockOutDate = now.Date.AddHours(12),
                    Items = new List<OrderItem>
                    {
                        new OrderItem { MaSach = 16, TenSach = "Cấu Trúc Dữ Liệu Và Giải Thuật", TacGia = "Lê Minh Hoàng", DonGia = 165000m, SoLuongMua = 2 },
                        new OrderItem { MaSach = 18, TenSach = "Muôn Kiếp Nhân Sinh", TacGia = "Nguyên Phong", DonGia = 168000m, SoLuongMua = 1 },
                        new OrderItem { MaSach = 1, TenSach = "Dế Mèn Phiêu Lưu Ký", TacGia = "Tô Hoài", DonGia = 85000m, SoLuongMua = 2 }
                    }
                },

                // #1009: Completed Day -1, COD Paid on delivery
                new Order
                {
                    MaDH = 1009,
                    MaND = 18,
                    HoTenKhachHang = "Võ Bảo Ngọc",
                    SDTKhachHang = "0933334455",
                    DiaChiGiaoHang = "205 Hai Bà Trưng, Quận 3, TP.HCM",
                    NgayDat = now.Date.AddDays(-1).AddHours(14),
                    NgayCapNhat = now.Date.AddDays(-1).AddHours(17),
                    TongTien = 528000m,
                    PaidAmount = 528000m,
                    Method = PaymentMethod.COD,
                    PaymentStatus = PaymentStatus.Paid,
                    PaymentNote = "[Mô phỏng]: Đã thu tiền hộ COD khi giao hàng thành công",
                    Status = OrderStatus.Completed,
                    StockOutStatus = StockOutStatus.Issued,
                    StockOutDate = now.Date.AddDays(-1).AddHours(15),
                    Items = new List<OrderItem>
                    {
                        new OrderItem { MaSach = 3, TenSach = "Nhà Giả Kim", TacGia = "Paulo Coelho", DonGia = 79000m, SoLuongMua = 4 },
                        new OrderItem { MaSach = 6, TenSach = "Đắc Nhân Tâm", TacGia = "Dale Carnegie", DonGia = 88000m, SoLuongMua = 1 },
                        new OrderItem { MaSach = 2, TenSach = "Tuổi Trẻ Đáng Giá Bao Nhiêu", TacGia = "Rosie Nguyễn", DonGia = 99000m, SoLuongMua = 1 },
                        new OrderItem { MaSach = 11, TenSach = "Số Đỏ", TacGia = "Vũ Trọng Phụng", DonGia = 25000m, SoLuongMua = 1 }
                    }
                },

                // #1010: Completed Day -2, BankTransfer Paid
                new Order
                {
                    MaDH = 1010,
                    MaND = 19,
                    HoTenKhachHang = "Đinh Anh Khoa",
                    SDTKhachHang = "0934445566",
                    DiaChiGiaoHang = "62 Phan Xích Long, Quận Phú Nhuận, TP.HCM",
                    NgayDat = now.Date.AddDays(-2).AddHours(10),
                    NgayCapNhat = now.Date.AddDays(-2).AddHours(15),
                    TongTien = 1090000m,
                    PaidAmount = 1090000m,
                    Method = PaymentMethod.BankTransfer,
                    PaymentStatus = PaymentStatus.Paid,
                    PaymentNote = "[Mô phỏng]: Đã xác nhận chuyển khoản",
                    Status = OrderStatus.Completed,
                    StockOutStatus = StockOutStatus.Issued,
                    StockOutDate = now.Date.AddDays(-2).AddHours(11),
                    Items = new List<OrderItem>
                    {
                        new OrderItem { MaSach = 4, TenSach = "Clean Code", TacGia = "Robert C. Martin", DonGia = 320000m, SoLuongMua = 2 },
                        new OrderItem { MaSach = 7, TenSach = "Tư Duy Nhanh Và Chậm", TacGia = "Daniel Kahneman", DonGia = 215000m, SoLuongMua = 1 },
                        new OrderItem { MaSach = 8, TenSach = "Sapiens: Lược Sử Loài Người", TacGia = "Yuval Noah Harari", DonGia = 235000m, SoLuongMua = 1 }
                    }
                },

                // #1011: Completed Day -3, Cash Paid
                new Order
                {
                    MaDH = 1011,
                    MaND = 21,
                    HoTenKhachHang = "Hồ Quang Vinh",
                    SDTKhachHang = "0936667788",
                    DiaChiGiaoHang = "33 Hoàng Diệu, Quận 4, TP.HCM",
                    NgayDat = now.Date.AddDays(-3).AddHours(9),
                    NgayCapNhat = now.Date.AddDays(-3).AddHours(14),
                    TongTien = 756000m,
                    PaidAmount = 756000m,
                    Method = PaymentMethod.Cash,
                    PaymentStatus = PaymentStatus.Paid,
                    PaymentNote = "Thu đủ tiền mặt: 756,000 ₫",
                    Status = OrderStatus.Completed,
                    StockOutStatus = StockOutStatus.Issued,
                    StockOutDate = now.Date.AddDays(-3).AddHours(11),
                    Items = new List<OrderItem>
                    {
                        new OrderItem { MaSach = 6, TenSach = "Đắc Nhân Tâm", TacGia = "Dale Carnegie", DonGia = 88000m, SoLuongMua = 4 },
                        new OrderItem { MaSach = 18, TenSach = "Muôn Kiếp Nhân Sinh", TacGia = "Nguyên Phong", DonGia = 168000m, SoLuongMua = 2 },
                        new OrderItem { MaSach = 12, TenSach = "Chí Phèo", TacGia = "Nam Cao", DonGia = 68000m, SoLuongMua = 1 }
                    }
                },

                // #1012: Completed Day -4, COD Paid on delivery
                new Order
                {
                    MaDH = 1012,
                    MaND = 22,
                    HoTenKhachHang = "Dương Tuấn Kiệt",
                    SDTKhachHang = "0937778899",
                    DiaChiGiaoHang = "150 Tô Hiến Thành, Quận 10, TP.HCM",
                    NgayDat = now.Date.AddDays(-4).AddHours(11),
                    NgayCapNhat = now.Date.AddDays(-4).AddHours(16),
                    TongTien = 890000m,
                    PaidAmount = 890000m,
                    Method = PaymentMethod.COD,
                    PaymentStatus = PaymentStatus.Paid,
                    PaymentNote = "[Mô phỏng]: Đã thu tiền hộ COD khi giao hàng",
                    Status = OrderStatus.Completed,
                    StockOutStatus = StockOutStatus.Issued,
                    StockOutDate = now.Date.AddDays(-4).AddHours(13),
                    Items = new List<OrderItem>
                    {
                        new OrderItem { MaSach = 9, TenSach = "Mắt Biếc", TacGia = "Nguyễn Nhật Ánh", DonGia = 110000m, SoLuongMua = 4 },
                        new OrderItem { MaSach = 10, TenSach = "Cho Tôi Xin Một Vé Đi Tuổi Thơ", TacGia = "Nguyễn Nhật Ánh", DonGia = 95000m, SoLuongMua = 2 },
                        new OrderItem { MaSach = 1, TenSach = "Dế Mèn Phiêu Lưu Ký", TacGia = "Tô Hoài", DonGia = 85000m, SoLuongMua = 3 },
                        new OrderItem { MaSach = 19, TenSach = "Hành Trình Về Phương Đông", TacGia = "Baird T. Spalding", DonGia = 5000m, SoLuongMua = 1 }
                    }
                },

                // #1013: Completed Day -5, BankTransfer Paid
                new Order
                {
                    MaDH = 1013,
                    MaND = 23,
                    HoTenKhachHang = "Cao Như Quỳnh",
                    SDTKhachHang = "0938889900",
                    DiaChiGiaoHang = "47 Nguyễn Thị Thập, Quận 7, TP.HCM",
                    NgayDat = now.Date.AddDays(-5).AddHours(10),
                    NgayCapNhat = now.Date.AddDays(-5).AddHours(15),
                    TongTien = 636000m,
                    PaidAmount = 636000m,
                    Method = PaymentMethod.BankTransfer,
                    PaymentStatus = PaymentStatus.Paid,
                    PaymentNote = "[Mô phỏng]: Đã xác nhận chuyển khoản",
                    Status = OrderStatus.Completed,
                    StockOutStatus = StockOutStatus.Issued,
                    StockOutDate = now.Date.AddDays(-5).AddHours(12),
                    Items = new List<OrderItem>
                    {
                        new OrderItem { MaSach = 3, TenSach = "Nhà Giả Kim", TacGia = "Paulo Coelho", DonGia = 79000m, SoLuongMua = 4 },
                        new OrderItem { MaSach = 4, TenSach = "Clean Code", TacGia = "Robert C. Martin", DonGia = 320000m, SoLuongMua = 1 }
                    }
                },

                // #1014: Completed Day -6, Cash Paid
                new Order
                {
                    MaDH = 1014,
                    MaND = 25,
                    HoTenKhachHang = "Bạch Khánh Linh",
                    SDTKhachHang = "0941122334",
                    DiaChiGiaoHang = "12 Cộng Hòa, Quận Tân Bình, TP.HCM",
                    NgayDat = now.Date.AddDays(-6).AddHours(9),
                    NgayCapNhat = now.Date.AddDays(-6).AddHours(14),
                    TongTien = 835000m,
                    PaidAmount = 835000m,
                    Method = PaymentMethod.Cash,
                    PaymentStatus = PaymentStatus.Paid,
                    PaymentNote = "Thu đủ tiền mặt: 835,000 ₫",
                    Status = OrderStatus.Completed,
                    StockOutStatus = StockOutStatus.Issued,
                    StockOutDate = now.Date.AddDays(-6).AddHours(11),
                    Items = new List<OrderItem>
                    {
                        new OrderItem { MaSach = 16, TenSach = "Cấu Trúc Dữ Liệu Và Giải Thuật", TacGia = "Lê Minh Hoàng", DonGia = 165000m, SoLuongMua = 3 },
                        new OrderItem { MaSach = 1, TenSach = "Dế Mèn Phiêu Lưu Ký", TacGia = "Tô Hoài", DonGia = 85000m, SoLuongMua = 4 }
                    }
                },

                // #1015: Shipping order, COD unpaid (should NOT count in realized revenue until collected)
                new Order
                {
                    MaDH = 1015,
                    MaND = 26,
                    HoTenKhachHang = "Đoàn Thành Đạt",
                    SDTKhachHang = "0942233445",
                    DiaChiGiaoHang = "72 Võ Văn Ngân, TP. Thủ Đức, TP.HCM",
                    NgayDat = now.Date.AddDays(-1).AddHours(16),
                    NgayCapNhat = now.Date.AddHours(8),
                    TongTien = 336000m,
                    Method = PaymentMethod.COD,
                    PaymentStatus = PaymentStatus.PayOnDelivery,
                    Status = OrderStatus.Shipping,
                    StockOutStatus = StockOutStatus.Issued,
                    StockOutDate = now.Date.AddHours(8),
                    Items = new List<OrderItem>
                    {
                        new OrderItem { MaSach = 18, TenSach = "Muôn Kiếp Nhân Sinh", TacGia = "Nguyên Phong", DonGia = 168000m, SoLuongMua = 2 }
                    }
                },

                // #1016: Cancelled order (must be excluded from revenue)
                new Order
                {
                    MaDH = 1016,
                    MaND = 27,
                    HoTenKhachHang = "Lâm Mỹ Duyên",
                    SDTKhachHang = "0943344556",
                    DiaChiGiaoHang = "128 Kinh Dương Vương, Quận 6, TP.HCM",
                    NgayDat = now.Date.AddDays(-3).AddHours(15),
                    NgayCapNhat = now.Date.AddDays(-3).AddHours(17),
                    TongTien = 410000m,
                    Method = PaymentMethod.BankTransfer,
                    PaymentStatus = PaymentStatus.Pending,
                    Status = OrderStatus.Cancelled,
                    StockOutStatus = StockOutStatus.NotIssued,
                    Items = new List<OrderItem>
                    {
                        new OrderItem { MaSach = 15, TenSach = "Design Patterns: Elements of Reusable OO Software", TacGia = "Erich Gamma et al.", DonGia = 410000m, SoLuongMua = 1 }
                    }
                }
            };

            foreach (var order in initialOrders)
            {
                Orders.Add(order);
            }
        }

        public IReadOnlyList<Book> GetAllBooks(bool includeInactive = false)
        {
            return includeInactive
                ? Books.ToList()
                : Books.Where(b => b.IsActive).ToList();
        }

        public Book? GetBookById(int maSach)
        {
            return Books.FirstOrDefault(b => b.MaSach == maSach);
        }

        public Order? GetOrderById(int maDH)
        {
            return Orders.FirstOrDefault(o => o.MaDH == maDH);
        }

        public Supplier? GetSupplierById(int maNCC)
        {
            return Suppliers.FirstOrDefault(s => s.MaNCC == maNCC);
        }

        public ImportReceipt? GetImportReceiptById(int maPN)
        {
            return ImportReceipts.FirstOrDefault(r => r.MaPN == maPN);
        }

        public IReadOnlyList<Book> SearchBooks(string keyword, bool includeInactive = false)
        {
            var source = includeInactive ? Books : Books.Where(b => b.IsActive);
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return source.ToList();
            }

            string normalized = keyword.Trim();
            string cleanId = normalized.TrimStart('#');
            if (cleanId.StartsWith("S", StringComparison.OrdinalIgnoreCase) && cleanId.Length > 1 && char.IsDigit(cleanId[1]))
            {
                cleanId = cleanId.Substring(1);
            }

            return source
                .Where(b => b.TenSach.Contains(normalized, StringComparison.OrdinalIgnoreCase)
                         || b.TacGia.Contains(normalized, StringComparison.OrdinalIgnoreCase)
                         || b.MaSach.ToString().Equals(cleanId, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public static bool IsValidPaymentMethod(string? paymentMethod)
        {
            if (string.IsNullOrWhiteSpace(paymentMethod))
            {
                return false;
            }

            string trimmed = paymentMethod.Trim();
            return trimmed.Equals("COD", StringComparison.OrdinalIgnoreCase)
                || trimmed.Equals("COD — Thanh toán khi nhận hàng", StringComparison.OrdinalIgnoreCase)
                || trimmed.Equals("Chuyển khoản", StringComparison.OrdinalIgnoreCase)
                || trimmed.Equals("Tiền mặt", StringComparison.OrdinalIgnoreCase);
        }

        public static string NormalizePaymentMethod(string paymentMethod)
        {
            return EnumExtensions.ParsePaymentMethod(paymentMethod).ToVietnameseLabel();
        }

        public OperationResult<Order> CreateOrder(
            string hoTen,
            string sdt,
            string diaChi,
            string phuongThucThanhToan,
            IEnumerable<CartItem> cartItems,
            int? authenticatedUserId = null)
        {
            // 1. Validate customer fields
            if (string.IsNullOrWhiteSpace(hoTen))
            {
                return OperationResult<Order>.Failure("Vui lòng nhập họ và tên khách hàng.");
            }

            if (string.IsNullOrWhiteSpace(sdt) || !PhoneRegex.IsMatch(sdt.Trim()))
            {
                return OperationResult<Order>.Failure("Số điện thoại không hợp lệ. Vui lòng nhập đúng 10 chữ số.");
            }

            if (string.IsNullOrWhiteSpace(diaChi))
            {
                return OperationResult<Order>.Failure("Vui lòng nhập địa chỉ giao hàng.");
            }

            // 2. Validate payment method
            if (!IsValidPaymentMethod(phuongThucThanhToan))
            {
                return OperationResult<Order>.Failure("Vui lòng chọn phương thức thanh toán hợp lệ (COD hoặc Chuyển khoản).");
            }

            // 3. Verify cart is not empty
            var itemsList = cartItems?.ToList() ?? new List<CartItem>();
            if (itemsList.Count == 0)
            {
                return OperationResult<Order>.Failure("Giỏ hàng đang trống. Không thể tạo đơn hàng.");
            }

            // 4. First pass: validate all cart items against shared books & latest stock BEFORE modifying any stock
            var validatedLines = new List<(Book SharedBook, int Quantity, decimal UnitPrice)>();

            foreach (var item in itemsList)
            {
                if (item.Book == null)
                {
                    return OperationResult<Order>.Failure("Phát hiện sản phẩm không hợp lệ trong giỏ hàng.");
                }

                var sharedBook = GetBookById(item.Book.MaSach);
                if (sharedBook == null || !sharedBook.IsActive)
                {
                    return OperationResult<Order>.Failure($"Sách \"{item.Book.TenSach}\" không còn kinh doanh trong hệ thống.");
                }

                if (item.SoLuongMua < 1)
                {
                    return OperationResult<Order>.Failure($"Số lượng mua của sách \"{sharedBook.TenSach}\" phải lớn hơn hoặc bằng 1.");
                }

                if (item.SoLuongMua > sharedBook.SoLuongTon)
                {
                    return OperationResult<Order>.Failure(
                        $"Sách \"{sharedBook.TenSach}\" chỉ còn {sharedBook.SoLuongTon} cuốn trong kho (bạn đang chọn {item.SoLuongMua} cuốn).");
                }

                validatedLines.Add((sharedBook, item.SoLuongMua, sharedBook.GiaBan));
            }

            // Check if duplicate book entries exist in cartItems that combined would exceed stock
            var groupedCheck = validatedLines
                .GroupBy(x => x.SharedBook.MaSach)
                .Select(g => new
                {
                    Book = g.First().SharedBook,
                    TotalRequested = g.Sum(x => x.Quantity)
                });

            foreach (var group in groupedCheck)
            {
                if (group.TotalRequested > group.Book.SoLuongTon)
                {
                    return OperationResult<Order>.Failure(
                        $"Tổng số lượng đặt cho sách \"{group.Book.TenSach}\" ({group.TotalRequested}) vượt quá số lượng tồn kho ({group.Book.SoLuongTon}).");
                }
            }

            // 5. All validations passed -> Deduct stock from shared Book instances
            decimal calculatedTotal = 0m;
            var orderSnapshots = new List<OrderItem>();

            foreach (var (sharedBook, quantity, unitPrice) in validatedLines)
            {
                sharedBook.SoLuongTon -= quantity;
                calculatedTotal += unitPrice * quantity;

                orderSnapshots.Add(new OrderItem
                {
                    MaSach = sharedBook.MaSach,
                    TenSach = sharedBook.TenSach,
                    TacGia = sharedBook.TacGia,
                    DonGia = unitPrice,
                    SoLuongMua = quantity
                });
            }

            // 6. Associate with authenticated customer or create customer record
            int maND;
            if (authenticatedUserId.HasValue)
            {
                maND = authenticatedUserId.Value;
                var existingUser = Users.FirstOrDefault(u => u.MaND == maND);
                if (existingUser != null)
                {
                    existingUser.HoTen = hoTen.Trim();
                    existingUser.SDT = sdt.Trim();
                    existingUser.DiaChi = diaChi.Trim();
                }
            }
            else
            {
                var newUser = new Customer
                {
                    MaND = GenerateNextUserId(),
                    HoTen = hoTen.Trim(),
                    SDT = sdt.Trim(),
                    DiaChi = diaChi.Trim(),
                    Role = UserRole.Customer,
                    IsActive = true
                };
                Users.Add(newUser);
                maND = newUser.MaND;
            }

            var parsedPaymentMethod = EnumExtensions.ParsePaymentMethod(phuongThucThanhToan);
            var initialPaymentStatus = parsedPaymentMethod == PaymentMethod.COD
                ? PaymentStatus.PayOnDelivery
                : PaymentStatus.Pending;

            // 7. Create mock Order record with snapshot
            var newOrder = new Order
            {
                MaDH = GenerateNextOrderId(),
                MaND = maND,
                HoTenKhachHang = hoTen.Trim(),
                SDTKhachHang = sdt.Trim(),
                DiaChiGiaoHang = diaChi.Trim(),
                NgayDat = DateTime.Now,
                NgayCapNhat = DateTime.Now,
                TongTien = calculatedTotal,
                Method = parsedPaymentMethod,
                PaymentStatus = initialPaymentStatus,
                Status = OrderStatus.New,
                StockOutStatus = StockOutStatus.NotIssued,
                Items = orderSnapshots
            };
            Orders.Insert(0, newOrder);
            NotifyDataChanged();

            return OperationResult<Order>.Success(
                newOrder,
                $"Đặt hàng thành công! Mã đơn hàng: #{newOrder.MaDH}");
        }
    }
}
