using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QuanlyNhaSach.Models
{
    public partial class StockOutLineItem : ObservableObject
    {
        private readonly Book? _sharedBook;

        public int MaDH { get; }
        public string OrderCode => $"#{MaDH}";
        public int MaSach { get; }
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

        public int CurrentStock => _sharedBook?.SoLuongTon ?? 0;

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

        public StockOutLineItem(int maDH, OrderItem item, Book? sharedBook, bool isAlreadyIssued)
        {
            MaDH = maDH;
            MaSach = item.MaSach;
            TenSach = item.TenSach;
            TacGia = item.TacGia;
            DonGia = item.DonGia;
            OrderedQuantity = item.SoLuongMua;
            IssueQuantity = item.SoLuongMua;
            _sharedBook = sharedBook;
            _isAlreadyIssued = isAlreadyIssued;

            if (_sharedBook != null)
            {
                PropertyChangedEventManager.AddHandler(_sharedBook, OnBookPropertyChanged, string.Empty);
            }
        }

        private void OnBookPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Book.SoLuongTon) || string.IsNullOrEmpty(e.PropertyName))
            {
                RefreshStockState();
            }
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
