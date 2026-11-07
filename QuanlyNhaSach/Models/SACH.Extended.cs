using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;

namespace QuanlyNhaSach.Models;

/// <summary>
/// Phần mở rộng (partial) của entity SACH do EF Core scaffold sinh ra.
/// Chỉ chứa các property CHỈ ĐỌC phục vụ hiển thị trên giao diện (không ánh xạ xuống DB).
/// KHÔNG sửa file SACH.cs gốc để tránh bị ghi đè khi scaffold lại.
/// </summary>
public partial class SACH
{
    /// <summary>Ngưỡng tồn kho thấp, đồng bộ với BookService (LowStockFilter: &lt; 10).</summary>
    public const int LowStockThreshold = 10;

    /// <summary>Màu bìa mặc định (DB không có cột màu bìa).</summary>
    public const string DefaultCoverColor = "#1E3A8A";

    private string? _mauBia;
    private string? _moTa;

    [NotMapped]
    public bool HasImage => !string.IsNullOrWhiteSpace(DisplayHinhAnh);

    [NotMapped]
    public string? DisplayHinhAnh => ResolveImagePath();

    private string? ResolveImagePath()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string binImgDir = Path.Combine(baseDir, "Img");
        string projectDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
        string projectImgDir = Path.Combine(projectDir, "Img");

        string? rawPath = Hinhanh?.Trim();

        // 1. Link online trực tiếp từ web
        if (!string.IsNullOrWhiteSpace(rawPath))
        {
            if (rawPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                rawPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return rawPath;
            }

            // 2. Đường dẫn file URI hoặc tệp tuyệt đối tồn tại
            if (rawPath.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            {
                return rawPath;
            }

            if (File.Exists(rawPath))
            {
                return Path.GetFullPath(rawPath);
            }

            // 3. Chuẩn hóa tên file (loại bỏ tiền tố /Img/, Img/, \Img\ nếu có)
            string cleanFileName = rawPath.TrimStart('/', '\\');
            if (cleanFileName.StartsWith("Img/", StringComparison.OrdinalIgnoreCase) ||
                cleanFileName.StartsWith("Img\\", StringComparison.OrdinalIgnoreCase))
            {
                cleanFileName = cleanFileName.Substring(4);
            }
            string fileNameOnly = Path.GetFileName(cleanFileName);

            // 4. Ưu tiên tìm trong thư mục Img tại output build (bin\Debug\...\Img)
            string binImgPath = Path.Combine(binImgDir, cleanFileName);
            if (File.Exists(binImgPath))
            {
                return binImgPath;
            }

            string binImgPathByName = Path.Combine(binImgDir, fileNameOnly);
            if (File.Exists(binImgPathByName))
            {
                return binImgPathByName;
            }

            // 5. Tìm trong thư mục Img của project nguồn
            string projectImgPath = Path.Combine(projectImgDir, cleanFileName);
            if (File.Exists(projectImgPath))
            {
                try
                {
                    Directory.CreateDirectory(binImgDir);
                    File.Copy(projectImgPath, binImgPath, true);
                }
                catch { }
                return projectImgPath;
            }

            string projectImgPathByName = Path.Combine(projectImgDir, fileNameOnly);
            if (File.Exists(projectImgPathByName))
            {
                try
                {
                    Directory.CreateDirectory(binImgDir);
                    File.Copy(projectImgPathByName, binImgPathByName, true);
                }
                catch { }
                return projectImgPathByName;
            }

            // 6. Tìm tại thư mục gốc bin
            string binRootPath = Path.Combine(baseDir, cleanFileName);
            if (File.Exists(binRootPath))
            {
                return binRootPath;
            }
        }

        // 7. Tự động nhận diện ảnh theo MaSach nếu có file ảnh sẵn trong thư mục Img
        if (!string.IsNullOrWhiteSpace(MaSach))
        {
            string code = MaSach.Trim();

            if (Directory.Exists(binImgDir))
            {
                var matches = Directory.GetFiles(binImgDir, $"{code}_*.*");
                if (matches.Length > 0)
                {
                    return matches[0];
                }
                var directMatches = Directory.GetFiles(binImgDir, $"{code}.*");
                if (directMatches.Length > 0)
                {
                    return directMatches[0];
                }
            }

            if (Directory.Exists(projectImgDir))
            {
                var matches = Directory.GetFiles(projectImgDir, $"{code}_*.*");
                if (matches.Length > 0)
                {
                    return matches[0];
                }
                var directMatches = Directory.GetFiles(projectImgDir, $"{code}.*");
                if (directMatches.Length > 0)
                {
                    return directMatches[0];
                }
            }
        }

        return null;
    }

    [NotMapped]
    public string MauBia
    {
        get => string.IsNullOrWhiteSpace(_mauBia) ? DefaultCoverColor : _mauBia!;
        set => _mauBia = string.IsNullOrWhiteSpace(value) ? DefaultCoverColor : value.Trim();
    }

    /// <summary>
    /// Mô tả nội dung sách (chỉ lưu tạm trong bộ nhớ vì bảng SACH chưa có cột MoTa).
    /// Tự động trả về chuỗi giới thiệu fallback an toàn khi null hoặc rỗng.
    /// </summary>
    [NotMapped]
    public string MoTa
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(_moTa))
            {
                return _moTa!;
            }

            string title = string.IsNullOrWhiteSpace(Tensach) ? "Đầu sách này" : $"\"{Tensach}\"";
            string author = string.IsNullOrWhiteSpace(Tacgia) ? "Đang cập nhật" : Tacgia!;
            string category = string.IsNullOrWhiteSpace(Theloai) ? "Tổng hợp" : Theloai!;
            string publisher = string.IsNullOrWhiteSpace(NXB) ? "Đang cập nhật" : NXB!;
            return $"{title} — Tác giả: {author} | Thể loại: {category} | NXB: {publisher}.";
        }
        set => _moTa = value?.Trim();
    }

    [NotMapped]
    public bool IsInStock => (Soluongton ?? 0) > 0;

    [NotMapped]
    public bool IsOutOfStock => !IsInStock;

    [NotMapped]
    public string FormattedPrice => $"{Giaban ?? 0m:N0} ₫";

    /// <summary>Trạng thái tồn kho hiển thị cho khách hàng / nhân viên.</summary>
    [NotMapped]
    public string StockStatusText
    {
        get
        {
            int stock = Soluongton ?? 0;
            if (stock <= 0)
            {
                return "Hết hàng";
            }
            return stock < LowStockThreshold ? $"Chỉ còn {stock} cuốn" : "Còn hàng";
        }
    }

    /// <summary>Trạng thái tồn kho hiển thị cho quản lý (khớp các bộ lọc của BookService).</summary>
    [NotMapped]
    public string AdminStockStatusText
    {
        get
        {
            int stock = Soluongton ?? 0;
            if (stock <= 0)
            {
                return "Hết hàng";
            }
            return stock < LowStockThreshold ? "Sắp hết" : "Đủ hàng";
        }
    }
}
