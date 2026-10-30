using CommunityToolkit.Mvvm.ComponentModel;

namespace QuanlyNhaSach.Models
{
    public partial class Supplier : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SupplierCode))]
        [NotifyPropertyChangedFor(nameof(DisplayName))]
        private int _maNCC;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayName))]
        private string _tenNCC = string.Empty;

        private string _sdt = string.Empty;
        public string SDT
        {
            get => _sdt;
            set => SetProperty(ref _sdt, value);
        }

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _diaChi = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusText))]
        [NotifyPropertyChangedFor(nameof(ToggleStatusActionText))]
        private bool _isActive = true;

        public string SupplierCode => $"NCC{MaNCC:D3}";
        public string DisplayName => $"{SupplierCode} — {TenNCC}";
        public string StatusText => IsActive ? "Hoạt động" : "Ngừng hợp tác";
        public string ToggleStatusActionText => IsActive ? "Ngừng hợp tác" : "Kích hoạt lại";
    }
}
