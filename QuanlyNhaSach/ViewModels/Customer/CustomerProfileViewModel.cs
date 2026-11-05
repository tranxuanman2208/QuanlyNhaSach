using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QuanlyNhaSach.Models;
using QuanlyNhaSach.Navigation;
using QuanlyNhaSach.Services;

namespace QuanlyNhaSach.ViewModels.Customer
{
    public partial class CustomerProfileViewModel : ObservableObject, INavigationAware
    {
        private readonly INavigationService? _navigationService;
        private readonly IDialogService? _dialogService;
        private readonly AuthService? _authService;

        private static readonly Dictionary<int, decimal> CachedOrderTotalsByMaDH = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CustomerName))]
        [NotifyPropertyChangedFor(nameof(AvatarInitials))]
        [NotifyPropertyChangedFor(nameof(CustomerPhone))]
        [NotifyPropertyChangedFor(nameof(CustomerEmail))]
        private NGUOIDUNG _currentUser;

        [ObservableProperty]
        private string _joinDateDisplay = "15/03/2025";

        [ObservableProperty]
        private DateTime _joinDate = new(2025, 3, 15);

        [ObservableProperty]
        private string _activeMenuKey = "AccountInfo";

        public ObservableCollection<DONHANG> Orders { get; } = new();

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

        public CustomerProfileViewModel() : this(null, null, null)
        {
        }

        public CustomerProfileViewModel(
            INavigationService? navigationService,
            IDialogService? dialogService,
            AuthService? authService = null)
        {
            _navigationService = navigationService;
            _dialogService = dialogService;
            _authService = authService;

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

            LoadCustomerAndOrdersFromDatabase();
        }

        public void OnNavigatedTo(object? parameter)
        {
            ActiveMenuKey = "AccountInfo";
            LoadCustomerAndOrdersFromDatabase();
        }

        private void LoadCustomerAndOrdersFromDatabase()
        {
            try
            {
                using var db = new QuanlyNhaSachContext();

                NGUOIDUNG? dbUser = null;
                var sessionUser = _authService?.CurrentUser;

                if (sessionUser != null)
                {
                    string? username = sessionUser.Username?.Trim();
                    string? phone = sessionUser.SDT?.Trim();

                    dbUser = db.NGUOIDUNGs
                        .AsNoTracking()
                        .FirstOrDefault(u =>
                            (!string.IsNullOrEmpty(username) && (u.Tendangnhap == username || u.Email == username)) ||
                            (!string.IsNullOrEmpty(phone) && u.SDT == phone));

                    if (dbUser == null)
                    {
                        dbUser = new NGUOIDUNG
                        {
                            MaND = $"KH{sessionUser.MaND:D3}",
                            Tendangnhap = username ?? "customer",
                            Hoten = sessionUser.HoTen,
                            SDT = sessionUser.SDT,
                            Email = username,
                            Diachi = sessionUser.DiaChi,
                            Vaitro = "Khách hàng",
                            Trangthai = "Hoạt động"
                        };
                    }
                }
                else
                {
                    dbUser = db.NGUOIDUNGs
                        .AsNoTracking()
                        .FirstOrDefault(u => u.Vaitro == "Khách hàng" || u.MaND.StartsWith("KH"));
                }

                if (dbUser != null)
                {
                    CurrentUser = dbUser;
                }

                var dbOrders = db.DONHANGs
                    .AsNoTracking()
                    .Include(d => d.CHITIETDONHANGs)
                    .Where(d => CurrentUser != null && d.MaKH == CurrentUser.MaND)
                    .OrderByDescending(d => d.NgayTao)
                    .ThenByDescending(d => d.MaDH)
                    .ToList();

                if (dbOrders.Count == 0)
                {
                    dbOrders = db.DONHANGs
                        .AsNoTracking()
                        .Include(d => d.CHITIETDONHANGs)
                        .OrderByDescending(d => d.NgayTao)
                        .ThenByDescending(d => d.MaDH)
                        .Take(10)
                        .ToList();
                }

                Orders.Clear();
                foreach (var order in dbOrders)
                {
                    CachedOrderTotalsByMaDH[order.MaDH] = order.TongTien;
                    Orders.Add(order);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi tải hồ sơ khách hàng từ CSDL: {ex.Message}");
            }
        }

        public static decimal GetMockOrderTotal(int maDh)
        {
            if (CachedOrderTotalsByMaDH.TryGetValue(maDh, out decimal cached) && cached > 0m)
            {
                return cached;
            }

            try
            {
                using var db = new QuanlyNhaSachContext();
                var order = db.DONHANGs
                    .AsNoTracking()
                    .Include(d => d.CHITIETDONHANGs)
                    .FirstOrDefault(d => d.MaDH == maDh);

                if (order != null)
                {
                    decimal total = order.TongTien;
                    CachedOrderTotalsByMaDH[maDh] = total;
                    return total;
                }
            }
            catch
            {
                // Ignore DB lookup errors in converter fallback
            }

            return 0m;
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

            char first = parts[^2][0];
            char second = parts[^1][0];
            return $"{first}{second}".ToUpperInvariant();
        }

        [RelayCommand]
        private void UpdateProfile()
        {
            try
            {
                if (CurrentUser != null && !string.IsNullOrWhiteSpace(CurrentUser.MaND))
                {
                    using var db = new QuanlyNhaSachContext();
                    var entity = db.NGUOIDUNGs.FirstOrDefault(u => u.MaND == CurrentUser.MaND);
                    if (entity != null)
                    {
                        entity.Hoten = CustomerName.Trim();
                        entity.SDT = CustomerPhone.Trim();
                        entity.Email = CustomerEmail.Trim();
                        db.SaveChanges();
                    }
                }

                if (_dialogService != null)
                {
                    _dialogService.ShowInformation("Thông báo", "Cập nhật thông tin tài khoản thành công!");
                }
                else
                {
                    MessageBox.Show(
                        "Cập nhật thông tin thành công!",
                        "Thông báo",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Không thể cập nhật thông tin: {ex.InnerException?.Message ?? ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
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
            LoadCustomerAndOrdersFromDatabase();
            MessageBox.Show(
                $"Hiện có {Orders.Count} đơn hàng trong lịch sử mua hàng của bạn.",
                "Lịch sử mua hàng",
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
            LoadCustomerAndOrdersFromDatabase();
            if (Orders.Count == 0)
            {
                MessageBox.Show(
                    "Bạn chưa có đơn hàng nào trong hệ thống.",
                    "Chi tiết đơn hàng",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var latest = Orders[0];
            MessageBox.Show(
                $"Đơn hàng gần nhất: {latest.OrderCode}\n" +
                $"Ngày đặt: {latest.FormattedNgayDat}\n" +
                $"Trạng thái: {latest.TrangThai}\n" +
                $"Tổng tiền: {latest.FormattedTongTien}",
                "Chi tiết đơn hàng",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
