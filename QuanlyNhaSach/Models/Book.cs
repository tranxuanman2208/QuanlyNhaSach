using CommunityToolkit.Mvvm.ComponentModel;

namespace QuanlyNhaSach.Models
{
    public partial class Book : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(BookCode))]
        private int _maSach;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShortTitle))]
        [NotifyPropertyChangedFor(nameof(CoverInitials))]
        private string _tenSach = string.Empty;

        [ObservableProperty]
        private string _tacGia = string.Empty;

        [ObservableProperty]
        private string _theLoai = "Văn học & Kỹ năng";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FormattedPrice))]
        private decimal _giaBan;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsInStock))]
        [NotifyPropertyChangedFor(nameof(IsOutOfStock))]
        [NotifyPropertyChangedFor(nameof(IsLowStock))]
        [NotifyPropertyChangedFor(nameof(IsLowOrOutOfStock))]
        [NotifyPropertyChangedFor(nameof(StockStatusText))]
        [NotifyPropertyChangedFor(nameof(AdminStockStatusText))]
        private int _soLuongTon;

        [ObservableProperty]
        private string _anhBia = string.Empty;

        [ObservableProperty]
        private string _mauBia = "#1E3A8A";

        [ObservableProperty]
        private string _moTa = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsInStock))]
        [NotifyPropertyChangedFor(nameof(StockStatusText))]
        [NotifyPropertyChangedFor(nameof(AdminStockStatusText))]
        private bool _isActive = true;

        public string BookCode => $"#{MaSach}";
        public bool IsInStock => IsActive && SoLuongTon > 0;
        public bool IsOutOfStock => !IsActive || SoLuongTon <= 0;

        /// <summary>
        /// Low-stock rule required by specification: SoLuongTon &lt; 10.
        /// </summary>
        public bool IsLowStock => SoLuongTon > 0 && SoLuongTon < 10;
        public bool IsLowOrOutOfStock => SoLuongTon < 10;

        public string FormattedPrice => $"{GiaBan:N0} ₫";

        public string StockStatusText => !IsActive
            ? "Ngừng kinh doanh"
            : (SoLuongTon <= 0
                ? "Hết hàng"
                : (SoLuongTon < 10
                    ? $"Sắp hết ({SoLuongTon} cuốn)"
                    : $"Còn hàng ({SoLuongTon} cuốn)"));

        public string AdminStockStatusText => !IsActive
            ? "Ngừng bán"
            : (SoLuongTon <= 0
                ? "Hết hàng"
                : (SoLuongTon < 10
                    ? "Sắp hết"
                    : "Bình thường"));

        public string ShortTitle => TenSach.Length > 34 ? TenSach.Substring(0, 31) + "..." : TenSach;

        public string CoverInitials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(TenSach))
                {
                    return "BK";
                }
                var words = TenSach.Trim().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
                if (words.Length == 1)
                {
                    return words[0].Substring(0, 1).ToUpperInvariant();
                }
                return $"{words[0][0]}{words[1][0]}".ToUpperInvariant();
            }
        }
    }
}
