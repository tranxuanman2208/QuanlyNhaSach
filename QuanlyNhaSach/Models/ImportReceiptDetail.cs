using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QuanlyNhaSach.Models
{
    public partial class ImportReceiptDetail : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(BookCode))]
        private int _maSach;

        [ObservableProperty]
        private string _tenSach = string.Empty;

        [ObservableProperty]
        private string _tacGia = string.Empty;

        [ObservableProperty]
        private int _currentStock;

        private int _soLuongNhap = 1;
        public int SoLuongNhap
        {
            get => _soLuongNhap;
            set
            {
                if (SetProperty(ref _soLuongNhap, value))
                {
                    OnPropertyChanged(nameof(ThanhTien));
                    OnPropertyChanged(nameof(FormattedThanhTien));
                    LineValueChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private decimal _donGiaNhap;
        public decimal DonGiaNhap
        {
            get => _donGiaNhap;
            set
            {
                if (SetProperty(ref _donGiaNhap, value))
                {
                    OnPropertyChanged(nameof(ThanhTien));
                    OnPropertyChanged(nameof(FormattedDonGiaNhap));
                    OnPropertyChanged(nameof(FormattedThanhTien));
                    LineValueChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler? LineValueChanged;

        public string BookCode => $"#{MaSach}";
        public decimal ThanhTien => _soLuongNhap > 0 && _donGiaNhap > 0 ? _soLuongNhap * _donGiaNhap : 0m;
        public string FormattedDonGiaNhap => $"{DonGiaNhap:N0} ₫";
        public string FormattedThanhTien => $"{ThanhTien:N0} ₫";
    }
}
