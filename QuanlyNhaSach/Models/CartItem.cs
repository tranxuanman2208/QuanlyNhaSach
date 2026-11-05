using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QuanlyNhaSach.Models
{
    public partial class CartItem : ObservableObject
    {
        private SACH _book;
        private int _soLuongMua = 1;

        public CartItem(SACH book, int soLuongMua = 1)
        {
            _book = book ?? throw new ArgumentNullException(nameof(book));
            // Xử lý tồn kho (byte?) có thể null từ DB
            int maxStock = Math.Max((byte)1, _book.Soluongton ?? 0);
            _soLuongMua = Math.Clamp(soLuongMua, 1, maxStock);
        }

        public SACH Book
        {
            get => _book;
            set
            {
                if (ReferenceEquals(_book, value) || value == null)
                {
                    return;
                }

                _book = value;

                OnPropertyChanged(nameof(Book));
                OnPropertyChanged(nameof(ThanhTien));
                OnPropertyChanged(nameof(FormattedThanhTien));
            }
        }

        public int SoLuongMua
        {
            get => _soLuongMua;
            set
            {
                if (value < 1)
                {
                    OnPropertyChanged(nameof(SoLuongMua));
                    return;
                }

                int tonKho = _book?.Soluongton ?? 0;

                if (_book != null && value > tonKho)
                {
                    OnPropertyChanged(nameof(SoLuongMua));
                    return;
                }

                if (SetProperty(ref _soLuongMua, value))
                {
                    OnPropertyChanged(nameof(ThanhTien));
                    OnPropertyChanged(nameof(FormattedThanhTien));
                }
            }
        }

        public decimal ThanhTien => _book != null ? (_book.Giaban ?? 0m) * _soLuongMua : 0m;

        public string FormattedThanhTien => $"{ThanhTien:N0} ₫";

        internal bool SetValidatedQuantity(int quantity)
        {
            int tonKho = _book?.Soluongton ?? 0;

            if (quantity < 1 || (_book != null && quantity > tonKho))
            {
                OnPropertyChanged(nameof(SoLuongMua));
                return false;
            }

            if (SetProperty(ref _soLuongMua, quantity, nameof(SoLuongMua)))
            {
                OnPropertyChanged(nameof(ThanhTien));
                OnPropertyChanged(nameof(FormattedThanhTien));
            }
            return true;
        }
    }
}