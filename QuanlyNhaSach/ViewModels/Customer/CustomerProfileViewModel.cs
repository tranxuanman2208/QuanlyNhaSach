using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QuanlyNhaSach.Models;
using QuanlyNhaSach.Models.Enums;
using QuanlyNhaSach.Navigation;
using QuanlyNhaSach.Services;

namespace QuanlyNhaSach.ViewModels.Customer
{
    public partial class CustomerProfileViewModel : ObservableObject, INavigationAware
    {
        private readonly INavigationService? _navigationService;
        private readonly IDialogService? _dialogService;
        private readonly AuthService? _authService;
        private readonly OrderService? _orderService;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CustomerName))]
        [NotifyPropertyChangedFor(nameof(AvatarInitials))]
        [NotifyPropertyChangedFor(nameof(CustomerPhone))]
        [NotifyPropertyChangedFor(nameof(CustomerEmail))]
        private NGUOIDUNG _currentUser;

        [ObservableProperty]
        private string _joinDateDisplay = DateTime.Today.ToString("dd/MM/yyyy");

        [ObservableProperty]
        private DateTime _joinDate = DateTime.Today;

        partial void OnJoinDateChanged(DateTime value)
        {
            _joinDateDisplay = value.ToString("dd/MM/yyyy");
            OnPropertyChanged(nameof(JoinDateDisplay));
        }

        partial void OnJoinDateDisplayChanged(string value)
        {
            if (DateTime.TryParseExact(value?.Trim(), new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd" },
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var parsed))
            {
                _joinDate = parsed;
                OnPropertyChanged(nameof(JoinDate));
            }
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsAccountInfoActive))]
        [NotifyPropertyChangedFor(nameof(IsPurchaseHistoryActive))]
        private string _activeMenuKey = "AccountInfo";

        public bool IsAccountInfoActive => ActiveMenuKey == "AccountInfo";
        public bool IsPurchaseHistoryActive => ActiveMenuKey == "PurchaseHistory";

        public ObservableCollection<DONHANG> Orders { get; } = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasSelectedHistoryOrder))]
        private DONHANG? _selectedHistoryOrder;

        public bool HasSelectedHistoryOrder => SelectedHistoryOrder != null;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasOrders))]
        [NotifyPropertyChangedFor(nameof(IsEmptyOrders))]
        private int _totalOrdersCount;

        [ObservableProperty]
        private decimal _totalSpent;

        [ObservableProperty]
        private string _formattedTotalSpent = "0 ₫";

        [ObservableProperty]
        private int _activeOrdersCount;

        public bool HasOrders => Orders.Count > 0;
        public bool IsEmptyOrders => Orders.Count == 0;

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

        public CustomerProfileViewModel() : this(null, null, null, null)
        {
        }

        public CustomerProfileViewModel(
            INavigationService? navigationService,
            IDialogService? dialogService,
            AuthService? authService = null,
            OrderService? orderService = null)
        {
            _navigationService = navigationService;
            _dialogService = dialogService;
            _authService = authService;
            _orderService = orderService;

            Orders.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(HasOrders));
                OnPropertyChanged(nameof(IsEmptyOrders));
            };

            if (_orderService != null)
            {
                _orderService.OrderUpdated += (_, _) =>
                {
                    var dispatcher = Application.Current?.Dispatcher;
                    if (dispatcher != null)
                    {
                        dispatcher.InvokeAsync(LoadCustomerAndOrdersFromDatabase);
                    }
                    else
                    {
                        LoadCustomerAndOrdersFromDatabase();
                    }
                };
            }

            _currentUser = _authService?.CurrentUser ?? new NGUOIDUNG();
            _joinDate = _currentUser.NgayThamGia;
            _joinDateDisplay = _joinDate.ToString("dd/MM/yyyy");

            LoadCustomerAndOrdersFromDatabase();
        }

        public void OnNavigatedTo(object? parameter)
        {
            if (parameter is string tabKey && tabKey.Equals("PurchaseHistory", StringComparison.OrdinalIgnoreCase))
            {
                NavigatePurchaseHistory();
            }
            else
            {
                ActiveMenuKey = "AccountInfo";
                LoadCustomerAndOrdersFromDatabase();
            }
        }

        public void LoadCustomerAndOrdersFromDatabase()
        {
            try
            {
                int? previousSelectedMaDH = SelectedHistoryOrder?.MaDH;

                using var db = new QuanlyNhaSachContext();

                NGUOIDUNG? dbUser = null;
                var sessionUser = _authService?.CurrentUser;

                if (sessionUser != null)
                {
                    string? currentMaND = sessionUser.MaND?.Trim();
                    string? username = sessionUser.Tendangnhap?.Trim();
                    string? phone = sessionUser.SDT?.Trim();
                    string? email = sessionUser.Email?.Trim();

                    if (!string.IsNullOrEmpty(currentMaND))
                    {
                        dbUser = db.NGUOIDUNGs
                            .AsNoTracking()
                            .FirstOrDefault(u => u.MaND == currentMaND);
                    }

                    dbUser ??= db.NGUOIDUNGs
                        .AsNoTracking()
                        .FirstOrDefault(u =>
                            (!string.IsNullOrEmpty(username) && (u.Tendangnhap == username || u.Email == username)) ||
                            (!string.IsNullOrEmpty(phone) && u.SDT == phone) ||
                            (!string.IsNullOrEmpty(email) && u.Email == email));

                    dbUser ??= sessionUser;
                }

                if (dbUser != null)
                {
                    CurrentUser = dbUser;
                    JoinDate = dbUser.NgayThamGia;
                    JoinDateDisplay = JoinDate.ToString("dd/MM/yyyy");
                }

                string? userMaND = CurrentUser?.MaND?.Trim();
                string? userPhone = CurrentUser?.SDT?.Trim();
                string? userEmail = CurrentUser?.Email?.Trim()?.ToLower();
                string? userLogin = CurrentUser?.Tendangnhap?.Trim()?.ToLower();

                List<DONHANG> dbOrders = new();
                if (!string.IsNullOrEmpty(userMaND) ||
                    !string.IsNullOrEmpty(userPhone) ||
                    !string.IsNullOrEmpty(userEmail) ||
                    !string.IsNullOrEmpty(userLogin))
                {
                    dbOrders = db.DONHANGs
                        .AsNoTracking()
                        .Include(d => d.MaKHNavigation)
                        .Include(d => d.CHITIETDONHANGs)
                            .ThenInclude(ct => ct.MaSachNavigation)
                        .Include(d => d.PHIEUXUATs)
                        .Where(d =>
                            (!string.IsNullOrEmpty(userMaND) && d.MaKH != null && d.MaKH == userMaND) ||
                            (!string.IsNullOrEmpty(userPhone) && d.MaKHNavigation != null && d.MaKHNavigation.SDT == userPhone) ||
                            (!string.IsNullOrEmpty(userEmail) && d.MaKHNavigation != null && d.MaKHNavigation.Email != null && d.MaKHNavigation.Email.ToLower() == userEmail) ||
                            (!string.IsNullOrEmpty(userLogin) && d.MaKHNavigation != null && d.MaKHNavigation.Tendangnhap.ToLower() == userLogin))
                        .OrderByDescending(d => d.NgayTao)
                        .ThenByDescending(d => d.MaDH)
                        .ToList();
                }

                Orders.Clear();
                foreach (var order in dbOrders)
                {
                    Orders.Add(order);
                }

                TotalOrdersCount = Orders.Count;
                TotalSpent = Orders.Where(o => o.Status == OrderStatus.Completed).Sum(o => o.TongTien);
                FormattedTotalSpent = $"{TotalSpent:N0} ₫";
                ActiveOrdersCount = Orders.Count(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled);

                OnPropertyChanged(nameof(HasOrders));
                OnPropertyChanged(nameof(IsEmptyOrders));

                if (Orders.Count > 0)
                {
                    SelectedHistoryOrder = (previousSelectedMaDH.HasValue
                        ? Orders.FirstOrDefault(o => o.MaDH == previousSelectedMaDH.Value)
                        : null) ?? Orders[0];
                }
                else
                {
                    SelectedHistoryOrder = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi tải hồ sơ khách hàng từ CSDL: {ex.Message}");
            }
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
            if (string.IsNullOrWhiteSpace(CustomerName))
            {
                ShowValidationError("Vui lòng nhập họ và tên.");
                return;
            }
            if (CustomerName.Trim().Length > 100)
            {
                ShowValidationError("Họ và tên không được vượt quá 100 ký tự.");
                return;
            }

            string cleanPhone = CustomerPhone?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(cleanPhone) || !Regex.IsMatch(cleanPhone, @"^\d{10}$"))
            {
                ShowValidationError("Số điện thoại không hợp lệ. Vui lòng nhập đúng 10 chữ số (0-9).");
                return;
            }

            string cleanEmail = CustomerEmail?.Trim().ToLowerInvariant() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(cleanEmail) || !cleanEmail.EndsWith("@gmail.com") || cleanEmail.Length < 11)
            {
                ShowValidationError("Email không hợp lệ. Vui lòng nhập đúng định dạng kết thúc bằng @gmail.com.");
                return;
            }

            DateTime finalJoinDate = JoinDate;
            if (DateTime.TryParseExact(JoinDateDisplay?.Trim(), new[] { "dd/MM/yyyy", "d/M/yyyy" },
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var parsedDate))
            {
                finalJoinDate = parsedDate;
            }

            try
            {
                if (CurrentUser != null && !string.IsNullOrWhiteSpace(CurrentUser.MaND))
                {
                    using var db = new QuanlyNhaSachContext();

                    bool duplicate = db.NGUOIDUNGs.Any(u =>
                        u.MaND != CurrentUser.MaND &&
                        ((u.Email != null && u.Email.ToLower() == cleanEmail) ||
                         (u.SDT != null && u.SDT == cleanPhone)));

                    if (duplicate)
                    {
                        ShowValidationError("Email hoặc Số điện thoại này đã được sử dụng bởi một tài khoản khác.");
                        return;
                    }

                    var entity = db.NGUOIDUNGs.FirstOrDefault(u => u.MaND == CurrentUser.MaND);
                    if (entity != null)
                    {
                        entity.Hoten = CustomerName.Trim();
                        entity.SDT = cleanPhone;
                        entity.Email = cleanEmail;
                        entity.NgayThamGia = finalJoinDate;
                        db.SaveChanges();

                        CurrentUser.Hoten = entity.Hoten;
                        CurrentUser.SDT = entity.SDT;
                        CurrentUser.Email = entity.Email;
                        CurrentUser.Trangthai = entity.Trangthai;

                        if (_authService?.CurrentUser != null)
                        {
                            _authService.CurrentUser.Hoten = entity.Hoten;
                            _authService.CurrentUser.SDT = entity.SDT;
                            _authService.CurrentUser.Email = entity.Email;
                            _authService.CurrentUser.Trangthai = entity.Trangthai;
                        }
                    }
                }

                if (_dialogService != null)
                {
                    _dialogService.ShowInformation("Thông báo", "Cập nhật thông tin tài khoản thành công!");
                }
                else
                {
                    MessageBox.Show(
                        "Cập nhật thông tin tài khoản thành công!",
                        "Thông báo",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                string msg = $"Không thể cập nhật thông tin: {ex.InnerException?.Message ?? ex.Message}";
                if (_dialogService != null)
                {
                    _dialogService.ShowError("Lỗi cập nhật", msg);
                }
                else
                {
                    MessageBox.Show(msg, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ShowValidationError(string message)
        {
            if (_dialogService != null)
            {
                _dialogService.ShowError("Lỗi dữ liệu", message);
            }
            else
            {
                MessageBox.Show(message, "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        [RelayCommand]
        private void Logout()
        {
            bool confirmed = _dialogService != null
                ? _dialogService.ShowConfirmation(
                    "Xác nhận đăng xuất",
                    "Bạn có chắc chắn muốn đăng xuất không?")
                : MessageBox.Show(
                    "Bạn có chắc chắn muốn đăng xuất  không?",
                    "Xác nhận đăng xuất",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes;

            if (!confirmed)
            {
                return;
            }

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
        public void NavigateAccountInfo()
        {
            ActiveMenuKey = "AccountInfo";
            LoadCustomerAndOrdersFromDatabase();
        }

        [RelayCommand]
        public void NavigatePurchaseHistory()
        {
            ActiveMenuKey = "PurchaseHistory";
            LoadCustomerAndOrdersFromDatabase();
            if (Orders.Count > 0 && SelectedHistoryOrder == null)
            {
                SelectedHistoryOrder = Orders[0];
            }
        }

        [RelayCommand]
        public void SelectHistoryOrder(DONHANG? order)
        {
            if (order != null)
            {
                SelectedHistoryOrder = order;
                ActiveMenuKey = "PurchaseHistory";
            }
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
    }
}
