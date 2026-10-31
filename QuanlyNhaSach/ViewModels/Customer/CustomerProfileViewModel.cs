using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanlyNhaSach.Models;
using QuanlyNhaSach.Navigation;
using QuanlyNhaSach.Services;

namespace QuanlyNhaSach.ViewModels.Customer
{
    public partial class CustomerProfileViewModel : ObservableObject, INavigationAware
    {
        private readonly INavigationService? _navigationService;
        private readonly IDialogService? _dialogService;

        /// <summary>
        /// Presentation-level mock order total mapping keyed by DONHANG.MaDH.
        /// Required because the scaffolded DONHANG table stores line totals in CHITIETDONHANG
        /// rather than a direct column on DONHANG.
        /// </summary>
        private static readonly IReadOnlyDictionary<int, decimal> MockOrderTotalsByMaDH = new Dictionary<int, decimal>
        {
            [1] = 250_000m,
            [2] = 180_000m,
            [3] = 320_000m
        };

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CustomerName))]
        [NotifyPropertyChangedFor(nameof(AvatarInitials))]
        [NotifyPropertyChangedFor(nameof(CustomerPhone))]
        [NotifyPropertyChangedFor(nameof(CustomerEmail))]
        private NGUOIDUNG _currentUser;

        /// <summary>
        /// ViewModel-only display property for the customer's join date,
        /// as the scaffolded NGUOIDUNG table does not include a join-date column.
        /// </summary>
        [ObservableProperty]
        private string _joinDateDisplay = "15/03/2025";

        [ObservableProperty]
        private DateTime _joinDate = new(2025, 3, 15);

        [ObservableProperty]
        private string _activeMenuKey = "AccountInfo";

        public ObservableCollection<DONHANG> Orders { get; }

        public string CustomerName
        {
            get => CurrentUser?.Hoten ?? string.Empty;
            set
            {
                if (CurrentUser != null && !string.Equals(CurrentUser.Hoten, value, StringComparison.Ordinal))
                {
                    CurrentUser.Hoten = value;
                    OnPropertyChanged(nameof(CustomerName));
                    OnPropertyChanged(nameof(AvatarInitials));
                }
            }
        }

        public string CustomerPhone
        {
            get => CurrentUser?.SDT ?? string.Empty;
            set
            {
                if (CurrentUser != null && !string.Equals(CurrentUser.SDT, value, StringComparison.Ordinal))
                {
                    CurrentUser.SDT = value;
                    OnPropertyChanged(nameof(CustomerPhone));
                }
            }
        }

        public string CustomerEmail
        {
            get => CurrentUser?.Email ?? string.Empty;
            set
            {
                if (CurrentUser != null && !string.Equals(CurrentUser.Email, value, StringComparison.Ordinal))
                {
                    CurrentUser.Email = value;
                    OnPropertyChanged(nameof(CustomerEmail));
                }
            }
        }

        public string AvatarInitials => ComputeInitials(CustomerName);

        public CustomerProfileViewModel() : this(null, null)
        {
        }

        public CustomerProfileViewModel(
            INavigationService? navigationService,
            IDialogService? dialogService)
        {
            _navigationService = navigationService;
            _dialogService = dialogService;

            // Initialize 1 mock NGUOIDUNG record using actual scaffolded properties
            _currentUser = new NGUOIDUNG
            {
                MaND = "KH001",
                Tendangnhap = "minhanh",
                Matkhau = "123456",
                Hoten = "Nguyễn Minh Anh",
                SDT = "0901234567",
                Email = "minhanh@example.com",
                Diachi = "123 Nguyễn Văn Cừ, Quận 5, TP.HCM",
                Vaitro = "Khách hàng",
                Trangthai = "Hoạt động"
            };

            _joinDate = new DateTime(2025, 3, 15);
            _joinDateDisplay = _joinDate.ToString("dd/MM/yyyy");

            // Initialize 3 mock DONHANG records using actual scaffolded properties
            Orders = new ObservableCollection<DONHANG>
            {
                new DONHANG
                {
                    MaDH = 1,
                    MaKH = "KH001",
                    NgayTao = new DateOnly(2026, 10, 20),
                    Phuongthucthanhtoan = "COD",
                    Trangthaigiaohang = "Hoàn tất"
                },
                new DONHANG
                {
                    MaDH = 2,
                    MaKH = "KH001",
                    NgayTao = new DateOnly(2026, 10, 25),
                    Phuongthucthanhtoan = "Chuyển khoản",
                    Trangthaigiaohang = "Đang xử lý"
                },
                new DONHANG
                {
                    MaDH = 3,
                    MaKH = "KH001",
                    NgayTao = new DateOnly(2026, 10, 29),
                    Phuongthucthanhtoan = "COD",
                    Trangthaigiaohang = "Đang giao"
                }
            };
        }

        public void OnNavigatedTo(object? parameter)
        {
            ActiveMenuKey = "AccountInfo";
        }

        public static decimal GetMockOrderTotal(int maDh)
        {
            return MockOrderTotalsByMaDH.TryGetValue(maDh, out decimal total) ? total : 0m;
        }

        public static string FormatOrderCode(int maDh)
        {
            return $"DH{maDh:D3}";
        }

        private static string ComputeInitials(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return "KH";
            }

            string[] parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
            {
                return parts[0].Substring(0, 1).ToUpperInvariant();
            }

            // Use last two name tokens for Vietnamese given names (e.g., "Nguyễn Minh Anh" -> "MA")
            char first = parts[^2][0];
            char second = parts[^1][0];
            return $"{first}{second}".ToUpperInvariant();
        }

        [RelayCommand]
        private void UpdateProfile()
        {
            MessageBox.Show(
                "Cập nhật thông tin thành công!",
                "Thông báo",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        [RelayCommand]
        private void Logout()
        {
            MessageBox.Show(
                "Đăng xuất thành công!",
                "Thông báo",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            _navigationService?.LogoutToAuthWindow();
        }

        [RelayCommand]
        private void NavigateHome()
        {
            if (_navigationService != null)
            {
                _navigationService.NavigateToHome();
                return;
            }

            MessageBox.Show(
                "Đang điều hướng về Trang chủ.",
                "Thông báo",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        [RelayCommand]
        private void NavigateAccountInfo()
        {
            ActiveMenuKey = "AccountInfo";
        }

        [RelayCommand]
        private void NavigatePurchaseHistory()
        {
            MessageBox.Show(
                "Chức năng Lịch sử mua hàng đang được phát triển trong phiên bản tiếp theo.",
                "Thông báo",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        [RelayCommand]
        private void NavigateFavoriteBooks()
        {
            MessageBox.Show(
                "Chức năng Sách yêu thích đang được phát triển trong phiên bản tiếp theo.",
                "Thông báo",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        [RelayCommand]
        private void ViewOrderDetails()
        {
            MessageBox.Show(
                "Chức năng Xem chi tiết đơn hàng đang được phát triển trong phiên bản tiếp theo.",
                "Thông báo",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
