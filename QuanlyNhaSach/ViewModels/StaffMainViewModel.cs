using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanlyNhaSach.Models;
using QuanlyNhaSach.Models.Enums;
using QuanlyNhaSach.Services;

namespace QuanlyNhaSach.ViewModels
{
    public partial class StaffMainViewModel : ObservableObject
    {
        private readonly AuthService _authService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;

        public string BrandName => "BookNest";
        public string PortalLabel => "CỔNG NHÂN VIÊN (STAFF PORTAL)";

        public string StaffFullName => _authService.CurrentUser?.HoTen ?? "Nhân viên";
        public string StaffUsername => _authService.CurrentUser?.Username ?? string.Empty;
        public string StaffRoleLabel => _authService.CurrentUser?.RoleDisplayName ?? "Nhân viên";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DashboardTag))]
        [NotifyPropertyChangedFor(nameof(LookupTag))]
        [NotifyPropertyChangedFor(nameof(SalesTag))]
        [NotifyPropertyChangedFor(nameof(OrderProcessingTag))]
        [NotifyPropertyChangedFor(nameof(StockOutTag))]
        [NotifyPropertyChangedFor(nameof(CurrentPageTitle))]
        [NotifyPropertyChangedFor(nameof(CurrentPageSubtitle))]
        private string _currentStaffPageKey = "Dashboard";

        public string DashboardTag => CurrentStaffPageKey == "Dashboard" ? "Active" : "Inactive";
        public string LookupTag => CurrentStaffPageKey == "Lookup" ? "Active" : "Inactive";
        public string SalesTag => CurrentStaffPageKey == "Sales" ? "Active" : "Inactive";
        public string OrderProcessingTag => CurrentStaffPageKey == "OrderProcessing" ? "Active" : "Inactive";
        public string StockOutTag => CurrentStaffPageKey == "StockOut" ? "Active" : "Inactive";

        public string CurrentPageTitle => CurrentStaffPageKey switch
        {
            "Dashboard" => "Trang chủ — Bảng Điều Khiển Nghiệp Vụ (UC05)",
            "Lookup" => "Tra Cứu Thông Tin Đơn Hàng & Sách",
            "Sales" => "Bán Hàng & Ghi Nhận Thanh Toán (UC08)",
            "OrderProcessing" => "Xử Lý Đơn Hàng & Luồng Trạng Thái (UC07)",
            "StockOut" => "Quản Lý Xuất Kho Đơn Hàng (UC10)",
            _ => "Cổng Nhân Viên BookNest"
        };

        public string CurrentPageSubtitle => CurrentStaffPageKey switch
        {
            "Dashboard" => "Tổng quan thống kê đơn hàng thời gian thực và danh sách đơn cần xử lý gấp.",
            "Lookup" => "Tìm kiếm đồng bộ theo mã đơn hàng, tên khách hàng, mã sách hoặc tên sách.",
            "Sales" => "Kiểm tra chi tiết đơn hàng và ghi nhận thanh toán Tiền mặt, Chuyển khoản hoặc COD.",
            "OrderProcessing" => "Quản lý trạng thái đơn hàng theo quy trình: Mới → Đã xác nhận → Đang giao → Hoàn tất.",
            "StockOut" => "Kiểm tra tồn kho thực tế và xác nhận xuất kho sách cho đơn hàng.",
            _ => string.Empty
        };

        public StaffMainViewModel(
            AuthService authService,
            INavigationService navigationService,
            IDialogService dialogService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            _navigationService.StaffNavigated += OnStaffNavigated;
            _authService.SessionChanged += OnSessionChanged;
        }

        public bool EnsureStaffAuthorized()
        {
            if (!_authService.HasRole(UserRole.Staff))
            {
                _dialogService.ShowError(
                    "Không có quyền truy cập",
                    "Chức năng này chỉ dành cho tài khoản Nhân viên hợp lệ. Hệ thống sẽ quay về màn hình Đăng nhập.");
                _navigationService.LogoutToAuthWindow();
                return false;
            }
            return true;
        }

        private void OnSessionChanged(object? sender, User? user)
        {
            OnPropertyChanged(nameof(StaffFullName));
            OnPropertyChanged(nameof(StaffUsername));
            OnPropertyChanged(nameof(StaffRoleLabel));
        }

        private void OnStaffNavigated(string pageKey)
        {
            CurrentStaffPageKey = pageKey;
        }

        [RelayCommand]
        private void NavigateDashboard()
        {
            if (!EnsureStaffAuthorized())
            {
                return;
            }
            _navigationService.NavigateToStaffDashboard();
        }

        [RelayCommand]
        private void NavigateLookup()
        {
            if (!EnsureStaffAuthorized())
            {
                return;
            }
            _navigationService.NavigateToStaffLookup();
        }

        [RelayCommand]
        private void NavigateSales()
        {
            if (!EnsureStaffAuthorized())
            {
                return;
            }
            _navigationService.NavigateToStaffSales();
        }

        [RelayCommand]
        private void NavigateOrderProcessing()
        {
            if (!EnsureStaffAuthorized())
            {
                return;
            }
            _navigationService.NavigateToStaffOrderProcessing();
        }

        [RelayCommand]
        private void NavigateStockOut()
        {
            if (!EnsureStaffAuthorized())
            {
                return;
            }
            _navigationService.NavigateToStaffStockOut();
        }

        [RelayCommand]
        private void Logout()
        {
            _navigationService.LogoutToAuthWindow();
        }
    }
}
