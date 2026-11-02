using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanlyNhaSach.Models;
using QuanlyNhaSach.Models.Enums;
using QuanlyNhaSach.Services;

namespace QuanlyNhaSach.ViewModels.Manager
{
    public partial class ManagerMainViewModel : ObservableObject
    {
        private readonly AuthService _authService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;

        public string BrandHeaderTitle => "NHÀ SÁCH NHÓM 1";
        public string PortalHeaderSubtitle => "CỔNG QUẢN TRỊ";

        public string ManagerFullName => _authService.CurrentUser?.HoTen ?? "Quản lý";
        public string ManagerInitials => _authService.CurrentUser?.Initials ?? "QL";
        public string ManagerUsername => _authService.CurrentUser?.Username ?? string.Empty;
        public string ManagerRoleLabel => "Quản lý";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(OverviewTag))]
        [NotifyPropertyChangedFor(nameof(BookManagementTag))]
        [NotifyPropertyChangedFor(nameof(StaffManagementTag))]
        [NotifyPropertyChangedFor(nameof(CustomerManagementTag))]
        [NotifyPropertyChangedFor(nameof(SupplierManagementTag))]
        [NotifyPropertyChangedFor(nameof(ImportReceiptTag))]
        [NotifyPropertyChangedFor(nameof(ImportHistoryTag))]
        [NotifyPropertyChangedFor(nameof(CurrentPageTitle))]
        [NotifyPropertyChangedFor(nameof(CurrentPageSubtitle))]
        private string _currentManagerPageKey = "Overview";

        public string OverviewTag => CurrentManagerPageKey == "Overview" ? "Active" : "Inactive";
        public string BookManagementTag => CurrentManagerPageKey == "BookManagement" ? "Active" : "Inactive";
        public string StaffManagementTag => CurrentManagerPageKey == "StaffManagement" ? "Active" : "Inactive";
        public string CustomerManagementTag => CurrentManagerPageKey == "CustomerManagement" ? "Active" : "Inactive";
        public string SupplierManagementTag => CurrentManagerPageKey == "SupplierManagement" ? "Active" : "Inactive";
        public string ImportReceiptTag => CurrentManagerPageKey == "ImportReceipt" ? "Active" : "Inactive";
        public string ImportHistoryTag => CurrentManagerPageKey == "ImportHistory" ? "Active" : "Inactive";

        public string CurrentPageTitle => CurrentManagerPageKey switch
        {
            "Overview" => "Tổng quan hoạt động kinh doanh (UC13)",
            "BookManagement" => "Quản lý danh mục sách & tồn kho (UC09)",
            "StaffManagement" => "Quản lý tài khoản nhân viên (UC02)",
            "CustomerManagement" => "Quản lý tài khoản khách hàng",
            "SupplierManagement" => "Quản lý đối tác & nhà cung cấp (UC12)",
            "ImportReceipt" => "Lập phiếu nhập hàng vào kho",
            "ImportHistory" => "Lịch sử phiếu nhập hàng",
            _ => "Cổng Quản Trị Nhà Sách Nhóm 1"
        };

        public string CurrentPageSubtitle => CurrentManagerPageKey switch
        {
            "Overview" => "Theo dõi tổng doanh thu thực thu, tổng đơn hàng, biểu đồ doanh thu theo thời gian và Top 5 sách bán chạy.",
            "BookManagement" => "Thêm mới, chỉnh sửa thông tin sách, theo dõi cảnh báo tồn kho thấp (< 10) và ngừng bán an toàn.",
            "StaffManagement" => "Tra cứu, lọc trạng thái, thêm tài khoản nhân viên mới và khóa/mở khóa quyền truy cập.",
            "CustomerManagement" => "Tra cứu thông tin khách hàng, khóa/mở khóa tài khoản và cấp lại mật khẩu tạm thời (Mô phỏng).",
            "SupplierManagement" => "Quản lý danh sách nhà cung cấp sách, thông tin liên hệ và bảo toàn lịch sử nhập hàng.",
            "ImportReceipt" => "Chọn nhà cung cấp, thêm sách nhập, tính thành tiền/tổng tiền tự động và xác nhận cộng tồn kho.",
            "ImportHistory" => "Tra cứu các phiếu nhập hàng đã xác nhận và xem chi tiết bản chụp (snapshot) lịch sử nhập kho.",
            _ => string.Empty
        };

        public ManagerMainViewModel(
            AuthService authService,
            INavigationService navigationService,
            IDialogService dialogService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            _navigationService.ManagerNavigated += OnManagerNavigated;
            _authService.SessionChanged += OnSessionChanged;
        }

        public bool EnsureManagerAuthorized()
        {
            if (!_authService.HasRole(UserRole.Manager))
            {
                _dialogService.ShowError(
                    "Không có quyền truy cập",
                    "Chức năng này chỉ dành cho tài khoản Quản lý (Manager) hợp lệ. Hệ thống sẽ quay về màn hình Đăng nhập.");
                _navigationService.LogoutToAuthWindow();
                return false;
            }
            return true;
        }

        private void OnSessionChanged(object? sender, User? user)
        {
            OnPropertyChanged(nameof(ManagerFullName));
            OnPropertyChanged(nameof(ManagerInitials));
            OnPropertyChanged(nameof(ManagerUsername));
            OnPropertyChanged(nameof(ManagerRoleLabel));
        }

        private void OnManagerNavigated(string pageKey)
        {
            CurrentManagerPageKey = pageKey;
        }

        [RelayCommand]
        private void NavigateOverview()
        {
            if (!EnsureManagerAuthorized())
            {
                return;
            }
            _navigationService.NavigateToManagerOverview();
        }

        [RelayCommand]
        private void NavigateBookManagement()
        {
            if (!EnsureManagerAuthorized())
            {
                return;
            }
            _navigationService.NavigateToBookManagement();
        }

        [RelayCommand]
        private void NavigateStaffManagement()
        {
            if (!EnsureManagerAuthorized())
            {
                return;
            }
            _navigationService.NavigateToStaffManagement();
        }

        [RelayCommand]
        private void NavigateCustomerManagement()
        {
            if (!EnsureManagerAuthorized())
            {
                return;
            }
            _navigationService.NavigateToCustomerManagement();
        }

        [RelayCommand]
        private void NavigateSupplierManagement()
        {
            if (!EnsureManagerAuthorized())
            {
                return;
            }
            _navigationService.NavigateToSupplierManagement();
        }

        [RelayCommand]
        private void NavigateImportReceipt()
        {
            if (!EnsureManagerAuthorized())
            {
                return;
            }
            _navigationService.NavigateToImportReceipt();
        }

        [RelayCommand]
        private void NavigateImportHistory()
        {
            if (!EnsureManagerAuthorized())
            {
                return;
            }
            _navigationService.NavigateToImportHistory();
        }

        [RelayCommand]
        private void Logout()
        {
            _navigationService.LogoutToAuthWindow();
        }
    }
}
