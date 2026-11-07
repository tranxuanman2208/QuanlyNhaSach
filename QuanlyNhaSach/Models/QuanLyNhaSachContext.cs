using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace QuanlyNhaSach.Models;

public class DbConnectionSettings
{
    public string Provider { get; set; } = "PostgreSQL";
    public string ServiceUri { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
}

public partial class QuanlyNhaSachContext : DbContext
{
    public const string DbSettingsFileName = "dbsettings.json";

    private static readonly object _connLock = new();
    private static string? _resolvedConnectionString;
    private static DateTime _lastOfflineCheck = DateTime.MinValue;
    private static bool _isDatabaseAvailable = true;
    private static bool _isSchemaInitialized = false;


    static QuanlyNhaSachContext()
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }

    public QuanlyNhaSachContext()
    {
    }

    public QuanlyNhaSachContext(DbContextOptions<QuanlyNhaSachContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CHITIETDONHANG> CHITIETDONHANGs { get; set; }

    public virtual DbSet<CHITIETGIOHANG> CHITIETGIOHANGs { get; set; }

    public virtual DbSet<CHITIETPHIEUNHAP> CHITIETPHIEUNHAPs { get; set; }

    public virtual DbSet<DONHANG> DONHANGs { get; set; }

    public virtual DbSet<GIOHANG> GIOHANGs { get; set; }

    public virtual DbSet<NGUOIDUNG> NGUOIDUNGs { get; set; }

    public virtual DbSet<NHACUNGCAP> NHACUNGCAPs { get; set; }

    public virtual DbSet<PHIEUNHAP> PHIEUNHAPs { get; set; }

    public virtual DbSet<PHIEUXUAT> PHIEUXUATs { get; set; }

    public virtual DbSet<SACH> SACHs { get; set; }

    public static string ConvertUriOrConnectionString(string rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            throw new ArgumentException("Chuỗi kết nối cơ sở dữ liệu trống.");
        }

        string trimmed = rawInput.Trim();
        if (trimmed.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(trimmed);
            string username = string.Empty;
            string password = string.Empty;

            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                var parts = uri.UserInfo.Split(':', 2);
                username = Uri.UnescapeDataString(parts[0]);
                if (parts.Length > 1)
                {
                    password = Uri.UnescapeDataString(parts[1]);
                }
            }

            string database = uri.AbsolutePath.Trim('/');
            if (string.IsNullOrWhiteSpace(database))
            {
                database = "defaultdb";
            }

            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.Port > 0 ? uri.Port : 5432,
                Database = database,
                Username = username,
                Password = password,
                SslMode = SslMode.Require,
                Pooling = true,
                Timeout = 10,
                CommandTimeout = 30
            };

            return builder.ConnectionString;
        }

        var connBuilder = new NpgsqlConnectionStringBuilder(trimmed)
        {
            Pooling = true
        };
        if (connBuilder.Timeout <= 0)
        {
            connBuilder.Timeout = 10;
        }
        return connBuilder.ConnectionString;
    }

    private static IEnumerable<string> GetCandidateConnectionStrings()
    {
        var results = new List<string>();

        try
        {
            string[] candidatePaths =
            {
                Path.Combine(AppContext.BaseDirectory, DbSettingsFileName),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..", DbSettingsFileName)),
                Path.Combine(Directory.GetCurrentDirectory(), DbSettingsFileName)
            };

            foreach (string path in candidatePaths)
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var settings = JsonSerializer.Deserialize<DbConnectionSettings>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true
                    });

                    if (settings != null)
                    {
                        if (!string.IsNullOrWhiteSpace(settings.ServiceUri))
                        {
                            results.Add(ConvertUriOrConnectionString(settings.ServiceUri));
                        }
                        if (!string.IsNullOrWhiteSpace(settings.ConnectionString))
                        {
                            results.Add(ConvertUriOrConnectionString(settings.ConnectionString));
                        }
                    }
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi đọc {DbSettingsFileName}: {ex.Message}");
        }


        return results;
    }

    private static string GetFastConnectionString()
    {
        lock (_connLock)
        {
            if (_resolvedConnectionString != null && _isDatabaseAvailable)
            {
                return _resolvedConnectionString;
            }

            if (!_isDatabaseAvailable && (DateTime.UtcNow - _lastOfflineCheck).TotalSeconds < 10)
            {
                throw new InvalidOperationException("Cơ sở dữ liệu đám mây đang ở chế độ chờ (Offline Fast-Fail).");
            }

            foreach (var candidate in GetCandidateConnectionStrings())
            {
                try
                {
                    using var conn = new NpgsqlConnection(candidate);
                    conn.Open();

                    if (!_isSchemaInitialized)
                    {
                        EnsurePostgreSqlSchemaAndSeed(conn);
                        _isSchemaInitialized = true;
                    }

                    _resolvedConnectionString = candidate;
                    _isDatabaseAvailable = true;
                    return _resolvedConnectionString;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Không thể kết nối PostgreSQL candidate: {ex.Message}");
                }
            }

            _isDatabaseAvailable = false;
            _lastOfflineCheck = DateTime.UtcNow;
            throw new InvalidOperationException("Không thể kết nối đến cơ sở dữ liệu đám mây PostgreSQL. Vui lòng kiểm tra kết nối Internet hoặc file dbsettings.json.");
        }
    }

    private static void EnsurePostgreSqlSchemaAndSeed(NpgsqlConnection conn)
    {
        const string createSchemaSql = @"
CREATE TABLE IF NOT EXISTS ""NHACUNGCAP"" (
    ""MaNCC"" varchar(10) NOT NULL,
    ""TenNCC"" varchar(100) NULL,
    ""Email"" varchar(100) NULL,
    ""SDT"" varchar(10) NULL,
    ""Diachi"" varchar(100) NULL,
    CONSTRAINT ""PK_MANCC"" PRIMARY KEY (""MaNCC"")
);

CREATE TABLE IF NOT EXISTS ""NGUOIDUNG"" (
    ""MaND"" varchar(10) NOT NULL,
    ""Tendangnhap"" varchar(10) NOT NULL,
    ""Matkhau"" varchar(60) NOT NULL,
    ""Hoten"" varchar(100) NULL,
    ""SDT"" varchar(10) NULL,
    ""Email"" varchar(100) NULL,
    ""Diachi"" varchar(100) NULL,
    ""Vaitro"" varchar(50) NULL,
    ""Trangthai"" varchar(50) NULL,
    CONSTRAINT ""PK__NGUOIDUN__2725D724"" PRIMARY KEY (""MaND"")
);

CREATE TABLE IF NOT EXISTS ""SACH"" (
    ""MaSach"" varchar(10) NOT NULL,
    ""Tensach"" varchar(100) NULL,
    ""Tacgia"" varchar(100) NULL,
    ""NXB"" varchar(100) NULL,
    ""Giaban"" numeric(10, 2) NULL,
    ""Soluongton"" smallint NULL,
    ""Theloai"" varchar(100) NULL,
    ""MaNCC"" varchar(10) NULL,
    ""Daxoa"" boolean NULL,
    ""Hinhanh"" varchar(500) NULL,
    CONSTRAINT ""PK__SACH__B235742D"" PRIMARY KEY (""MaSach""),
    CONSTRAINT ""fk_MaNCC"" FOREIGN KEY (""MaNCC"") REFERENCES ""NHACUNGCAP"" (""MaNCC"")
);

CREATE TABLE IF NOT EXISTS ""GIOHANG"" (
    ""MaGH"" integer GENERATED BY DEFAULT AS IDENTITY NOT NULL,
    ""MaKH"" varchar(10) NULL,
    ""NgayCapNhat"" timestamp without time zone NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT ""PK__GIOHANG__2725AE85"" PRIMARY KEY (""MaGH""),
    CONSTRAINT ""UQ__GIOHANG__2725CF1F8B45322B"" UNIQUE (""MaKH""),
    CONSTRAINT ""fk_MaKH_GH"" FOREIGN KEY (""MaKH"") REFERENCES ""NGUOIDUNG"" (""MaND"")
);

CREATE TABLE IF NOT EXISTS ""CHITIETGIOHANG"" (
    ""MaGH"" integer NOT NULL,
    ""MaSach"" varchar(10) NOT NULL,
    ""Soluong"" smallint NULL,
    ""Tong"" numeric(10, 2) NULL,
    CONSTRAINT ""pk_MaGH_MaSach"" PRIMARY KEY (""MaGH"", ""MaSach""),
    CONSTRAINT ""fk_MaGH_GH"" FOREIGN KEY (""MaGH"") REFERENCES ""GIOHANG"" (""MaGH""),
    CONSTRAINT ""fk_MaSach_GH"" FOREIGN KEY (""MaSach"") REFERENCES ""SACH"" (""MaSach"")
);

CREATE TABLE IF NOT EXISTS ""PHIEUNHAP"" (
    ""MaPN"" integer GENERATED BY DEFAULT AS IDENTITY NOT NULL,
    ""MaQly"" varchar(10) NULL,
    ""MaNCC"" varchar(10) NULL,
    ""NgayNhap"" date NOT NULL DEFAULT CURRENT_DATE,
    ""Dathanhtoan"" boolean NULL,
    ""Tongtien"" numeric(10, 2) NULL,
    CONSTRAINT ""PK__PHIEUNHA__2725E7F4"" PRIMARY KEY (""MaPN""),
    CONSTRAINT ""fk_MaNCC_PN"" FOREIGN KEY (""MaNCC"") REFERENCES ""NHACUNGCAP"" (""MaNCC""),
    CONSTRAINT ""FK_MAQLY_PN"" FOREIGN KEY (""MaQly"") REFERENCES ""NGUOIDUNG"" (""MaND"")
);

CREATE TABLE IF NOT EXISTS ""CHITIETPHIEUNHAP"" (
    ""MaPN"" integer NOT NULL,
    ""MaSach"" varchar(10) NOT NULL,
    ""Soluongnhap"" smallint NULL,
    ""Tong"" numeric(10, 2) NULL,
    CONSTRAINT ""pk_MaSach_MaPN"" PRIMARY KEY (""MaPN"", ""MaSach""),
    CONSTRAINT ""fk_MaPN"" FOREIGN KEY (""MaPN"") REFERENCES ""PHIEUNHAP"" (""MaPN""),
    CONSTRAINT ""fk_MaSach_PN"" FOREIGN KEY (""MaSach"") REFERENCES ""SACH"" (""MaSach"")
);

CREATE TABLE IF NOT EXISTS ""DONHANG"" (
    ""MaDH"" integer GENERATED BY DEFAULT AS IDENTITY NOT NULL,
    ""MaKH"" varchar(10) NULL,
    ""NgayTao"" date NOT NULL DEFAULT CURRENT_DATE,
    ""Phuongthucthanhtoan"" varchar(100) NULL,
    ""Trangthaigiaohang"" varchar(100) NULL,
    ""Dathanhtoan"" boolean NULL,
    ""Tongtien"" numeric(10, 2) NULL,
    CONSTRAINT ""PK__DONHANG__27258661"" PRIMARY KEY (""MaDH""),
    CONSTRAINT ""fk_MaKH_DH"" FOREIGN KEY (""MaKH"") REFERENCES ""NGUOIDUNG"" (""MaND"")
);

CREATE TABLE IF NOT EXISTS ""CHITIETDONHANG"" (
    ""MaDH"" integer NOT NULL,
    ""MaSach"" varchar(10) NOT NULL,
    ""Soluong"" smallint NULL,
    ""Tong"" numeric(10, 2) NULL,
    CONSTRAINT ""pk_MaDH_MaSach"" PRIMARY KEY (""MaDH"", ""MaSach""),
    CONSTRAINT ""fk_MaDH_DH"" FOREIGN KEY (""MaDH"") REFERENCES ""DONHANG"" (""MaDH""),
    CONSTRAINT ""fk_MaSach_DH"" FOREIGN KEY (""MaSach"") REFERENCES ""SACH"" (""MaSach"")
);

CREATE TABLE IF NOT EXISTS ""PHIEUXUAT"" (
    ""MaPX"" integer GENERATED BY DEFAULT AS IDENTITY NOT NULL,
    ""MaDH"" integer NULL,
    ""Ghichu"" varchar(200) NULL,
    ""NgayXuat"" date NOT NULL DEFAULT CURRENT_DATE,
    CONSTRAINT ""PK__PHIEUXUA__2725E7F2"" PRIMARY KEY (""MaPX""),
    CONSTRAINT ""FK_MaDH"" FOREIGN KEY (""MaDH"") REFERENCES ""DONHANG"" (""MaDH"")
);
";

        using (var createCmd = new NpgsqlCommand(createSchemaSql, conn))
        {
            createCmd.ExecuteNonQuery();
        }

        using var checkCmd = new NpgsqlCommand(@"SELECT COUNT(*) FROM ""NGUOIDUNG"";", conn);
        long userCount = Convert.ToInt64(checkCmd.ExecuteScalar() ?? 0L);
        if (userCount > 0)
        {
            return;
        }

        const string seedDataSql = @"
INSERT INTO ""NHACUNGCAP"" (""MaNCC"", ""TenNCC"", ""Email"", ""SDT"", ""Diachi"") VALUES
('NCC0001', 'Kim Hiền', 'tranman22082007@gmail.com', '0789474034', '173 đường 3/2 phường Vườn Lài Thành phố Hồ Chí Minh')
ON CONFLICT (""MaNCC"") DO NOTHING;

INSERT INTO ""NGUOIDUNG"" (""MaND"", ""Tendangnhap"", ""Matkhau"", ""Hoten"", ""SDT"", ""Email"", ""Diachi"", ""Vaitro"", ""Trangthai"") VALUES
('KH001', 'customer12', '$2a$11$LQ8gKA.u3vbKDWPCm04QAO8SvvLzJbNnIGELfn5ZsuPWBtBN0k0Gu', 'Trần Huy Hoàng', '0389144650', 'tranman22082007@gmail.com', '280 adv phường chợ quán', 'Khách hàng', 'Hoạt động (07/11/2026)'),
('KH002', 'KH002', '$2a$11$rX6Ev7nwdIKWWXPDp2a/yeK2nxVTk7Wer23aIF7qr04bXZPQWjA8C', 'Nguyễn Hải Đăng', '0123456789', 'manxanhbike2208@gmail.com', '920 nguyễn thị định phường cát lái tphcm', 'Khách hàng', 'Hoạt động (08/11/2026)'),
('KH003', 'uchiha', '$2a$11$8Y1TiJUtdYu3.J082Jdwauhg9Mo0Xhhswy2NavOpM3O4AK4hzAioK', 'Nguyễn Hải Bằng', '0789474035', 'haibang01.clone@gmail.com', '135 đường 3/2 phường Vườn Lài TPHCM', 'Khách hàng', 'Hoạt động (08/11/2026)'),
('NV001', 'staff123', '$2a$11$Tj56lI8hqReusPhIoe4yMuqlE80z0KrzEPobh4yOsazCLguhkcMIW', 'Nguyễn Huyền Quang', '0799360399', 'tranman22082008@gmail.com', '540 nguyễn trãi tphcm', 'Nhân viên', 'Hoạt động (07/11/2026)'),
('QL001', 'manager123', '$2a$11$RA/4Aw.3JdjdVnH4Co0j/.Mx4oNKjSgHlK/SgIFJ7OZws8n7K5qlS', 'Trần Khải Vy', '0789474034', 'roidajasan@gmail.com', '345 nguyễn văn trỗi tphcm', 'Quản lý', 'Hoạt động (07/11/2026)')
ON CONFLICT (""MaND"") DO NOTHING;

INSERT INTO ""SACH"" (""MaSach"", ""Tensach"", ""Tacgia"", ""NXB"", ""Giaban"", ""Soluongton"", ""Theloai"", ""MaNCC"", ""Daxoa"", ""Hinhanh"") VALUES
('MS001', 'Doraemon tập 1', 'Fujiko F Fujio', 'Kim Đồng', 20000.00, 12, 'Truyện tranh', 'NCC0001', false, 'MS001_Doraemon_FujiKo_F_Fujio.jpg')
ON CONFLICT (""MaSach"") DO NOTHING;

INSERT INTO ""PHIEUNHAP"" (""MaPN"", ""MaQly"", ""MaNCC"", ""NgayNhap"", ""Dathanhtoan"", ""Tongtien"") VALUES
(1, 'QL001', 'NCC0001', '2026-11-07', true, 14000.00)
ON CONFLICT (""MaPN"") DO NOTHING;

INSERT INTO ""CHITIETPHIEUNHAP"" (""MaPN"", ""MaSach"", ""Soluongnhap"", ""Tong"") VALUES
(1, 'MS001', 1, 14000.00)
ON CONFLICT (""MaPN"", ""MaSach"") DO NOTHING;

INSERT INTO ""DONHANG"" (""MaDH"", ""MaKH"", ""NgayTao"", ""Phuongthucthanhtoan"", ""Trangthaigiaohang"", ""Dathanhtoan"", ""Tongtien"") VALUES
(1, 'KH001', '2026-11-07', 'COD', 'Hoàn tất', true, 20000.00),
(2, 'KH001', '2026-11-07', 'Cash', 'Hoàn tất', true, 60000.00),
(3, 'KH001', '2026-11-08', 'BankTransfer', 'Đã xác nhận', true, 20000.00),
(4, 'KH003', '2026-11-08', 'BankTransfer', 'Đã xác nhận', true, 20000.00),
(6, 'KH002', '2026-11-08', 'BankTransfer', 'Đã xác nhận', true, 20000.00)
ON CONFLICT (""MaDH"") DO NOTHING;

INSERT INTO ""CHITIETDONHANG"" (""MaDH"", ""MaSach"", ""Soluong"", ""Tong"") VALUES
(1, 'MS001', 1, 20000.00),
(2, 'MS001', 3, 60000.00),
(3, 'MS001', 1, 20000.00),
(4, 'MS001', 1, 20000.00),
(6, 'MS001', 1, 20000.00)
ON CONFLICT (""MaDH"", ""MaSach"") DO NOTHING;

INSERT INTO ""PHIEUXUAT"" (""MaPX"", ""MaDH"", ""Ghichu"", ""NgayXuat"") VALUES
(1, 1, 'Xuất kho 1 cuốn cho đơn hàng #DH0001', '2026-11-07'),
(2, 2, 'Xuất kho 3 cuốn cho đơn hàng #DH0002', '2026-11-08')
ON CONFLICT (""MaPX"") DO NOTHING;

SELECT setval(pg_get_serial_sequence('""PHIEUNHAP""', 'MaPN'), COALESCE((SELECT MAX(""MaPN"") FROM ""PHIEUNHAP""), 1), true);
SELECT setval(pg_get_serial_sequence('""DONHANG""', 'MaDH'), COALESCE((SELECT MAX(""MaDH"") FROM ""DONHANG""), 1), true);
SELECT setval(pg_get_serial_sequence('""PHIEUXUAT""', 'MaPX'), COALESCE((SELECT MAX(""MaPX"") FROM ""PHIEUXUAT""), 1), true);
";

        using var seedCmd = new NpgsqlCommand(seedDataSql, conn);
        seedCmd.ExecuteNonQuery();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql(GetFastConnectionString());
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CHITIETDONHANG>(entity =>
        {
            entity.HasKey(e => new { e.MaDH, e.MaSach }).HasName("pk_MaDH_MaSach");

            entity.ToTable("CHITIETDONHANG");

            entity.Property(e => e.MaSach)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Tong).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.MaDHNavigation).WithMany(p => p.CHITIETDONHANGs)
                .HasForeignKey(d => d.MaDH)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_MaDH_DH");

            entity.HasOne(d => d.MaSachNavigation).WithMany(p => p.CHITIETDONHANGs)
                .HasForeignKey(d => d.MaSach)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_MaSach_DH");
        });

        modelBuilder.Entity<CHITIETGIOHANG>(entity =>
        {
            entity.HasKey(e => new { e.MaGH, e.MaSach }).HasName("pk_MaGH_MaSach");

            entity.ToTable("CHITIETGIOHANG");

            entity.Property(e => e.MaSach)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Tong).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.MaGHNavigation).WithMany(p => p.CHITIETGIOHANGs)
                .HasForeignKey(d => d.MaGH)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_MaGH_GH");

            entity.HasOne(d => d.MaSachNavigation).WithMany(p => p.CHITIETGIOHANGs)
                .HasForeignKey(d => d.MaSach)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_MaSach_GH");
        });

        modelBuilder.Entity<CHITIETPHIEUNHAP>(entity =>
        {
            entity.HasKey(e => new { e.MaPN, e.MaSach }).HasName("pk_MaSach_MaPN");

            entity.ToTable("CHITIETPHIEUNHAP");

            entity.Property(e => e.MaSach)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Tong).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.MaPNNavigation).WithMany(p => p.CHITIETPHIEUNHAPs)
                .HasForeignKey(d => d.MaPN)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_MaPN");

            entity.HasOne(d => d.MaSachNavigation).WithMany(p => p.CHITIETPHIEUNHAPs)
                .HasForeignKey(d => d.MaSach)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_MaSach_PN");
        });

        modelBuilder.Entity<DONHANG>(entity =>
        {
            entity.HasKey(e => e.MaDH).HasName("PK__DONHANG__27258661");

            entity.ToTable("DONHANG");

            entity.Property(e => e.MaKH)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.NgayTao).HasDefaultValueSql("CURRENT_DATE");
            entity.Property(e => e.Phuongthucthanhtoan)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Trangthaigiaohang).HasMaxLength(100);
            entity.Property(e => e.Tongtien).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.MaKHNavigation).WithMany(p => p.DONHANGs)
                .HasForeignKey(d => d.MaKH)
                .HasConstraintName("fk_MaKH_DH");
        });

        modelBuilder.Entity<GIOHANG>(entity =>
        {
            entity.HasKey(e => e.MaGH).HasName("PK__GIOHANG__2725AE85");

            entity.ToTable("GIOHANG");

            entity.HasIndex(e => e.MaKH, "UQ__GIOHANG__2725CF1F8B45322B").IsUnique();

            entity.Property(e => e.MaKH)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.NgayCapNhat).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.MaKHNavigation).WithMany(p => p.GIOHANGs)
                .HasForeignKey(d => d.MaKH)
                .HasConstraintName("fk_MaKH_GH");
        });

        modelBuilder.Entity<NGUOIDUNG>(entity =>
        {
            entity.HasKey(e => e.MaND).HasName("PK__NGUOIDUN__2725D724");

            entity.ToTable("NGUOIDUNG");
            entity.Ignore(e => e.Role);
            entity.Ignore(e => e.IsActive);
            entity.Ignore(e => e.UserCode);
            entity.Ignore(e => e.RoleDisplayName);
            entity.Ignore(e => e.AccountStatusLabel);
            entity.Ignore(e => e.ToggleLockActionText);
            entity.Ignore(e => e.Initials);
            entity.Ignore(e => e.NgayThamGia);

            entity.Property(e => e.MaND)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Diachi).HasMaxLength(100);
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Hoten).HasMaxLength(100);
            entity.Property(e => e.Matkhau)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.SDT)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Tendangnhap)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Trangthai).HasMaxLength(50);
            entity.Property(e => e.Vaitro).HasMaxLength(50);
        });

        modelBuilder.Entity<NHACUNGCAP>(entity =>
        {
            entity.HasKey(e => e.MaNCC).HasName("PK_MANCC");

            entity.ToTable("NHACUNGCAP");

            entity.Property(e => e.MaNCC)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Diachi).HasMaxLength(100);
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.SDT)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.TenNCC).HasMaxLength(100);
        });

        modelBuilder.Entity<PHIEUNHAP>(entity =>
        {
            entity.HasKey(e => e.MaPN).HasName("PK__PHIEUNHA__2725E7F4");

            entity.ToTable("PHIEUNHAP");

            entity.Property(e => e.MaNCC)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MaQly)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.NgayNhap).HasDefaultValueSql("CURRENT_DATE");
            entity.Property(e => e.Tongtien).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.MaNCCNavigation).WithMany(p => p.PHIEUNHAPs)
                .HasForeignKey(d => d.MaNCC)
                .HasConstraintName("fk_MaNCC_PN");

            entity.HasOne(d => d.MaQlyNavigation).WithMany(p => p.PHIEUNHAPs)
                .HasForeignKey(d => d.MaQly)
                .HasConstraintName("FK_MAQLY_PN");
        });

        modelBuilder.Entity<PHIEUXUAT>(entity =>
        {
            entity.HasKey(e => e.MaPX).HasName("PK__PHIEUXUA__2725E7F2");

            entity.ToTable("PHIEUXUAT");

            entity.Property(e => e.Ghichu).HasMaxLength(200);
            entity.Property(e => e.NgayXuat).HasDefaultValueSql("CURRENT_DATE");

            entity.HasOne(d => d.MaDHNavigation).WithMany(p => p.PHIEUXUATs)
                .HasForeignKey(d => d.MaDH)
                .HasConstraintName("FK_MaDH");
        });

        modelBuilder.Entity<SACH>(entity =>
        {
            entity.HasKey(e => e.MaSach).HasName("PK__SACH__B235742D");

            entity.ToTable("SACH");

            entity.Ignore(e => e.HasImage);
            entity.Ignore(e => e.DisplayHinhAnh);
            entity.Ignore(e => e.MauBia);
            entity.Ignore(e => e.MoTa);
            entity.Ignore(e => e.IsInStock);
            entity.Ignore(e => e.IsOutOfStock);
            entity.Ignore(e => e.FormattedPrice);
            entity.Ignore(e => e.StockStatusText);
            entity.Ignore(e => e.AdminStockStatusText);

            entity.Property(e => e.MaSach)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Giaban).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.MaNCC)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.NXB).HasMaxLength(100);
            entity.Property(e => e.Tacgia).HasMaxLength(100);
            entity.Property(e => e.Tensach).HasMaxLength(100);
            entity.Property(e => e.Theloai).HasMaxLength(100);
            entity.Property(e => e.Hinhanh).HasMaxLength(500);

            entity.HasOne(d => d.MaNCCNavigation).WithMany(p => p.SACHs)
                .HasForeignKey(d => d.MaNCC)
                .HasConstraintName("fk_MaNCC");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
