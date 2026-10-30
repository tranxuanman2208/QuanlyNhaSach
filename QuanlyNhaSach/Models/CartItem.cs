using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QuanlyNhaSach.Models
{
    public partial class CartItem : ObservableObject
    {
        private Book _book;
        private int _soLuongMua = 1;

        public CartItem(Book book, int soLuongMua = 1)
        {
            _book = book ?? throw new ArgumentNullException(nameof(book));
            SubscribeToBook(_book);

            int maxStock = Math.Max(1, _book.SoLuongTon);
            _soLuongMua = Math.Clamp(soLuongMua, 1, maxStock);
        }

        public Book Book
        {
            get => _book;
            set
            {
                if (ReferenceEquals(_book, value) || value == null)
                {
                    return;
                }

                UnsubscribeFromBook(_book);
                _book = value;
                SubscribeToBook(_book);

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

                if (_book != null && value > _book.SoLuongTon)
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

        public decimal ThanhTien => _book != null ? _book.GiaBan * _soLuongMua : 0m;

        public string FormattedThanhTien => $"{ThanhTien:N0} ₫";

        internal bool SetValidatedQuantity(int quantity)
        {
            if (quantity < 1 || (_book != null && quantity > _book.SoLuongTon))
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

        private void SubscribeToBook(Book? book)
        {
            if (book != null)
            {
                PropertyChangedEventManager.AddHandler(book, OnBookPropertyChanged, string.Empty);
            }
        }

        private void UnsubscribeFromBook(Book? book)
        {
            if (book != null)
            {
                PropertyChangedEventManager.RemoveHandler(book, OnBookPropertyChanged, string.Empty);
            }
        }

        private void OnBookPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Book.GiaBan) || string.IsNullOrEmpty(e.PropertyName))
            {
                OnPropertyChanged(nameof(ThanhTien));
                OnPropertyChanged(nameof(FormattedThanhTien));
            }
            if (e.PropertyName == nameof(Book.SoLuongTon) || string.IsNullOrEmpty(e.PropertyName))
            {
                OnPropertyChanged(nameof(Book));
            }
        }
    }
}
