using System.ComponentModel.DataAnnotations.Schema;

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

    /// <summary>Mã placeholder ảnh bìa mặc định (DB không có cột ảnh bìa).</summary>
    public const string DefaultCoverPlaceholder = "BookCoverPlaceholder";

    private string? _mauBia;
    private string? _anhBia;
    private string? _moTa;

    [NotMapped]
    public string MauBia
    {
        get => string.IsNullOrWhiteSpace(_mauBia) ? DefaultCoverColor : _mauBia!;
        set => _mauBia = string.IsNullOrWhiteSpace(value) ? DefaultCoverColor : value.Trim();
    }

    [NotMapped]
    public string AnhBia
    {
        get => string.IsNullOrWhiteSpace(_anhBia) ? DefaultCoverPlaceholder : _anhBia!;
        set => _anhBia = string.IsNullOrWhiteSpace(value) ? DefaultCoverPlaceholder : value.Trim();
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
