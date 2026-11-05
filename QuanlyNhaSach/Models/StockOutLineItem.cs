using CommunityToolkit.Mvvm.ComponentModel;

namespace QuanlyNhaSach.Models
{
    public partial class StockOutLineItem : ObservableObject
    {
        private SACH? _sharedBook;

        public int MaDH { get; }
        public string OrderCode => $"#DH{MaDH:D4}";
        public string MaSach { get; }
        public string TenSach { get; }
        public string TacGia { get; }
        public decimal DonGia { get; }
        public int OrderedQuantity { get; }
        public int IssueQuantity { get; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsInsufficientStock))]
        [NotifyPropertyChangedFor(nameof(IsSufficientStock))]
        [NotifyPropertyChangedFor(nameof(ShortageQuantity))]
        [NotifyPropertyChangedFor(nameof(AvailabilityStatusText))]
        [NotifyPropertyChangedFor(nameof(AvailabilityDetailText))]
        private bool _isAlreadyIssued;

        public int CurrentStock => _sharedBook?.Soluongton ?? 0;

        public bool IsInsufficientStock => !IsAlreadyIssued && CurrentStock < IssueQuantity;
        public bool IsSufficientStock => IsAlreadyIssued || CurrentStock >= IssueQuantity;
        public int ShortageQuantity => IsInsufficientStock ? IssueQuantity - CurrentStock : 0;

        public string AvailabilityStatusText
        {
            get
            {
                if (IsAlreadyIssued)
                {
                    return "Đã xuất kho";
                }
                return IsInsufficientStock ? "Thiếu tồn kho" : "Đủ tồn kho";
            }
        }

        public string AvailabilityDetailText
        {
            get
            {
                if (IsAlreadyIssued)
                {
                    return $"Đã xuất {IssueQuantity} cuốn (Tồn hiện tại: {CurrentStock})";
                }
                if (IsInsufficientStock)
                {
                    return $"Thiếu {ShortageQuantity} cuốn (Yêu cầu: {IssueQuantity}, Tồn kho: {CurrentStock})";
                }
                return $"Đủ hàng (Yêu cầu: {IssueQuantity}, Tồn kho: {CurrentStock})";
            }
        }

        public StockOutLineItem(int maDH, CHITIETDONHANG item, SACH? sharedBook, bool isAlreadyIssued)
        {
            MaDH = maDH;
            MaSach = item?.MaSach ?? sharedBook?.MaSach ?? string.Empty;
            TenSach = !string.IsNullOrWhiteSpace(item?.TenSach)
                ? item.TenSach
                : (sharedBook?.Tensach ?? $"Sách #{MaSach}");
            TacGia = !string.IsNullOrWhiteSpace(item?.TacGia)
                ? item.TacGia
                : (sharedBook?.Tacgia ?? string.Empty);
            DonGia = item?.DonGia ?? (sharedBook?.Giaban ?? 0m);
            OrderedQuantity = item?.SoLuongMua ?? 0;
            IssueQuantity = item?.SoLuongMua ?? 0;

            _sharedBook = sharedBook;
            _isAlreadyIssued = isAlreadyIssued;
        }

        public StockOutLineItem(int maDH, SACH? sharedBook, bool isAlreadyIssued)
        {
            MaDH = maDH;
            MaSach = sharedBook?.MaSach ?? string.Empty;
            TenSach = sharedBook?.Tensach ?? string.Empty;
            TacGia = sharedBook?.Tacgia ?? string.Empty;
            DonGia = sharedBook?.Giaban ?? 0m;
            OrderedQuantity = 0;
            IssueQuantity = 0;

            _sharedBook = sharedBook;
            _isAlreadyIssued = isAlreadyIssued;
        }

        public void UpdateBookSnapshot(SACH? updatedBook)
        {
            _sharedBook = updatedBook;
            RefreshStockState();
        }

        public void RefreshStockState()
        {
            OnPropertyChanged(nameof(CurrentStock));
            OnPropertyChanged(nameof(IsInsufficientStock));
            OnPropertyChanged(nameof(IsSufficientStock));
            OnPropertyChanged(nameof(ShortageQuantity));
            OnPropertyChanged(nameof(AvailabilityStatusText));
            OnPropertyChanged(nameof(AvailabilityDetailText));
        }
    }
}
