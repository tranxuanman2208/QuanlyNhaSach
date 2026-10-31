using System;
using System.Windows;
using System.Windows.Controls;
using QuanlyNhaSach.Models;
using QuanlyNhaSach.Models.Enums;
using QuanlyNhaSach.Navigation;
using QuanlyNhaSach.ViewModels;
using QuanlyNhaSach.ViewModels.Customer;
using QuanlyNhaSach.ViewModels.Manager;
using QuanlyNhaSach.ViewModels.Staff;
using QuanlyNhaSach.Views.Auth;
using QuanlyNhaSach.Views.Customer;
using QuanlyNhaSach.Views.Manager;
using QuanlyNhaSach.Views.Staff;

namespace QuanlyNhaSach.Services
{
    public class NavigationService : INavigationService
    {
        private Frame? _customerFrame;
        private Frame? _staffFrame;
        private Frame? _managerFrame;

        private readonly MockDataService _mockDataService;
        private readonly AuthService _authService;
        private readonly CartService _cartService;
        private readonly OrderService _orderService;
        private readonly InventoryService _inventoryService;
        private readonly IDialogService _dialogService;
        private readonly BookService _bookService;
        private readonly UserManagementService _userManagementService;
        private readonly SupplierService _supplierService;
        private readonly ImportService _importService;

        // Customer cached pages & ViewModels
        private HomePage? _homePage;
        private HomeViewModel? _homeViewModel;

        private BookDetailPage? _bookDetailPage;
        private BookDetailViewModel? _bookDetailViewModel;

        private CartCheckoutPage? _cartCheckoutPage;
        private CartCheckoutViewModel? _cartCheckoutViewModel;

        private CustomerProfilePage? _customerProfilePage;
        private CustomerProfileViewModel? _customerProfileViewModel;

        // Staff cached pages & ViewModels
        private StaffDashboardPage? _staffDashboardPage;
        private StaffDashboardViewModel? _staffDashboardViewModel;

        private StaffLookupPage? _staffLookupPage;
        private StaffLookupViewModel? _staffLookupViewModel;

        private StaffSalesPage? _staffSalesPage;
        private StaffSalesViewModel? _staffSalesViewModel;

        private StaffOrderProcessingPage? _staffOrderProcessingPage;
        private StaffOrderProcessingViewModel? _staffOrderProcessingViewModel;

        private StaffStockOutPage? _staffStockOutPage;
        private StaffStockOutViewModel? _staffStockOutViewModel;

        // Manager cached pages & ViewModels
        private ManagerOverviewPage? _managerOverviewPage;
        private ManagerOverviewViewModel? _managerOverviewViewModel;

        private BookManagementPage? _bookManagementPage;
        private BookManagementViewModel? _bookManagementViewModel;

        private StaffManagementPage? _staffManagementPage;
        private StaffManagementViewModel? _staffManagementViewModel;

        private CustomerManagementPage? _customerManagementPage;
        private CustomerManagementViewModel? _customerManagementViewModel;

        private SupplierManagementPage? _supplierManagementPage;
        private SupplierManagementViewModel? _supplierManagementViewModel;

        private ImportReceiptPage? _importReceiptPage;
        private ImportReceiptViewModel? _importReceiptViewModel;

        private ImportHistoryPage? _importHistoryPage;
        private ImportHistoryViewModel? _importHistoryViewModel;

        public string CurrentPageKey { get; private set; } = "Home";
        public string CurrentStaffPageKey { get; private set; } = "Dashboard";
        public string CurrentManagerPageKey { get; private set; } = "Overview";

        public event Action<string>? Navigated;
        public event Action<string>? StaffNavigated;
        public event Action<string>? ManagerNavigated;

        public NavigationService(
            MockDataService mockDataService,
            AuthService authService,
            CartService cartService,
            OrderService orderService,
            InventoryService inventoryService,
            IDialogService dialogService,
            BookService? bookService = null,
            UserManagementService? userManagementService = null,
            SupplierService? supplierService = null,
            ImportService? importService = null)
        {
            _mockDataService = mockDataService ?? throw new ArgumentNullException(nameof(mockDataService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _cartService = cartService ?? throw new ArgumentNullException(nameof(cartService));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            _bookService = bookService ?? new BookService(_mockDataService);
            _userManagementService = userManagementService ?? new UserManagementService(_mockDataService);
            _supplierService = supplierService ?? new SupplierService(_mockDataService);
            _importService = importService ?? new ImportService(_mockDataService);
        }

        // ==================== CUSTOMER NAVIGATION ====================

        public void Initialize(Frame frame)
        {
            _customerFrame = frame ?? throw new ArgumentNullException(nameof(frame));
            NavigateToHome();
        }

        public void NavigateToHome(string? searchKeyword = null)
        {
            if (!EnsureCustomerAccess())
            {
                return;
            }

            CurrentPageKey = "Home";

            if (_homeViewModel == null)
            {
                _homeViewModel = new HomeViewModel(_mockDataService, _cartService, this, _dialogService);
            }

            if (_customerFrame != null && _homePage == null)
            {
                _homePage = new HomePage(_homeViewModel);
            }

            if (_homeViewModel is INavigationAware aware)
            {
                aware.OnNavigatedTo(searchKeyword);
            }

            if (_homePage != null)
            {
                NavigateFrame(_customerFrame, _homePage);
            }

            Navigated?.Invoke(CurrentPageKey);
        }

        public void NavigateToBookDetail(int maSach)
        {
            if (!EnsureCustomerAccess())
            {
                return;
            }

            CurrentPageKey = "BookDetail";

            if (_bookDetailViewModel == null)
            {
                _bookDetailViewModel = new BookDetailViewModel(_mockDataService, _cartService, this, _dialogService);
            }

            if (_customerFrame != null && _bookDetailPage == null)
            {
                _bookDetailPage = new BookDetailPage(_bookDetailViewModel);
            }

            if (_bookDetailViewModel is INavigationAware aware)
            {
                aware.OnNavigatedTo(maSach);
            }

            if (_bookDetailPage != null)
            {
                NavigateFrame(_customerFrame, _bookDetailPage);
            }

            Navigated?.Invoke(CurrentPageKey);
        }

        public void NavigateToCartCheckout()
        {
            if (!EnsureCustomerAccess())
            {
                return;
            }

            CurrentPageKey = "CartCheckout";

            if (_cartCheckoutViewModel == null)
            {
                _cartCheckoutViewModel = new CartCheckoutViewModel(
                    _mockDataService,
                    _cartService,
                    this,
                    _dialogService,
                    _authService,
                    _orderService);
            }

            if (_customerFrame != null && _cartCheckoutPage == null)
            {
                _cartCheckoutPage = new CartCheckoutPage(_cartCheckoutViewModel);
            }

            if (_cartCheckoutViewModel is INavigationAware aware)
            {
                aware.OnNavigatedTo(null);
            }

            if (_cartCheckoutPage != null)
            {
                NavigateFrame(_customerFrame, _cartCheckoutPage);
            }

            Navigated?.Invoke(CurrentPageKey);
        }

        public void NavigateToCustomerProfile()
        {
            if (!EnsureCustomerAccess())
            {
                return;
            }

            CurrentPageKey = "CustomerProfile";

            if (_customerProfileViewModel == null)
            {
                _customerProfileViewModel = new CustomerProfileViewModel(this, _dialogService);
            }

            if (_customerFrame != null && _customerProfilePage == null)
            {
                _customerProfilePage = new CustomerProfilePage(_customerProfileViewModel);
            }

            if (_customerProfileViewModel is INavigationAware aware)
            {
                aware.OnNavigatedTo(null);
            }

            if (_customerProfilePage != null)
            {
                NavigateFrame(_customerFrame, _customerProfilePage);
            }

            Navigated?.Invoke(CurrentPageKey);
        }

        // ==================== STAFF NAVIGATION ====================

        public void InitializeStaffFrame(Frame frame)
        {
            _staffFrame = frame ?? throw new ArgumentNullException(nameof(frame));
            NavigateToStaffDashboard();
        }

        public void NavigateToStaffDashboard()
        {
            if (!EnsureStaffAccess())
            {
                return;
            }

            CurrentStaffPageKey = "Dashboard";

            if (_staffDashboardViewModel == null)
            {
                _staffDashboardViewModel = new StaffDashboardViewModel(_mockDataService, _orderService, this);
            }

            if (_staffFrame != null && _staffDashboardPage == null)
            {
                _staffDashboardPage = new StaffDashboardPage(_staffDashboardViewModel);
            }

            _staffDashboardViewModel.OnNavigatedTo(null);

            if (_staffDashboardPage != null)
            {
                NavigateFrame(_staffFrame, _staffDashboardPage);
            }

            StaffNavigated?.Invoke(CurrentStaffPageKey);
        }

        public void NavigateToStaffLookup(string? initialKeyword = null)
        {
            if (!EnsureStaffAccess())
            {
                return;
            }

            CurrentStaffPageKey = "Lookup";

            if (_staffLookupViewModel == null)
            {
                _staffLookupViewModel = new StaffLookupViewModel(_mockDataService, _orderService, this);
            }

            if (_staffFrame != null && _staffLookupPage == null)
            {
                _staffLookupPage = new StaffLookupPage(_staffLookupViewModel);
            }

            _staffLookupViewModel.OnNavigatedTo(initialKeyword);

            if (_staffLookupPage != null)
            {
                NavigateFrame(_staffFrame, _staffLookupPage);
            }

            StaffNavigated?.Invoke(CurrentStaffPageKey);
        }

        public void NavigateToStaffSales(int? orderId = null)
        {
            if (!EnsureStaffAccess())
            {
                return;
            }

            CurrentStaffPageKey = "Sales";

            if (_staffSalesViewModel == null)
            {
                _staffSalesViewModel = new StaffSalesViewModel(_mockDataService, _orderService, this, _dialogService);
            }

            if (_staffFrame != null && _staffSalesPage == null)
            {
                _staffSalesPage = new StaffSalesPage(_staffSalesViewModel);
            }

            _staffSalesViewModel.OnNavigatedTo(orderId);

            if (_staffSalesPage != null)
            {
                NavigateFrame(_staffFrame, _staffSalesPage);
            }

            StaffNavigated?.Invoke(CurrentStaffPageKey);
        }

        public void NavigateToStaffOrderProcessing(int? orderId = null)
        {
            if (!EnsureStaffAccess())
            {
                return;
            }

            CurrentStaffPageKey = "OrderProcessing";

            if (_staffOrderProcessingViewModel == null)
            {
                _staffOrderProcessingViewModel = new StaffOrderProcessingViewModel(
                    _mockDataService,
                    _orderService,
                    this,
                    _dialogService);
            }

            if (_staffFrame != null && _staffOrderProcessingPage == null)
            {
                _staffOrderProcessingPage = new StaffOrderProcessingPage(_staffOrderProcessingViewModel);
            }

            _staffOrderProcessingViewModel.OnNavigatedTo(orderId);

            if (_staffOrderProcessingPage != null)
            {
                NavigateFrame(_staffFrame, _staffOrderProcessingPage);
            }

            StaffNavigated?.Invoke(CurrentStaffPageKey);
        }

        public void NavigateToStaffStockOut(int? orderId = null)
        {
            if (!EnsureStaffAccess())
            {
                return;
            }

            CurrentStaffPageKey = "StockOut";

            if (_staffStockOutViewModel == null)
            {
                _staffStockOutViewModel = new StaffStockOutViewModel(
                    _mockDataService,
                    _orderService,
                    _inventoryService,
                    this,
                    _dialogService);
            }

            if (_staffFrame != null && _staffStockOutPage == null)
            {
                _staffStockOutPage = new StaffStockOutPage(_staffStockOutViewModel);
            }

            _staffStockOutViewModel.OnNavigatedTo(orderId);

            if (_staffStockOutPage != null)
            {
                NavigateFrame(_staffFrame, _staffStockOutPage);
            }

            StaffNavigated?.Invoke(CurrentStaffPageKey);
        }

        // ==================== MANAGER NAVIGATION (7 ROUTES) ====================

        public void InitializeManagerFrame(Frame frame)
        {
            _managerFrame = frame ?? throw new ArgumentNullException(nameof(frame));
            NavigateToManagerOverview();
        }

        public void NavigateToManagerOverview()
        {
            if (!EnsureManagerAccess())
            {
                return;
            }

            CurrentManagerPageKey = "Overview";

            if (_managerOverviewViewModel == null)
            {
                _managerOverviewViewModel = new ManagerOverviewViewModel(
                    _mockDataService,
                    _orderService,
                    _bookService,
                    this);
            }

            if (_managerFrame != null && _managerOverviewPage == null)
            {
                _managerOverviewPage = new ManagerOverviewPage(_managerOverviewViewModel);
            }

            _managerOverviewViewModel.OnNavigatedTo(null);

            if (_managerOverviewPage != null)
            {
                NavigateFrame(_managerFrame, _managerOverviewPage);
            }

            ManagerNavigated?.Invoke(CurrentManagerPageKey);
        }

        public void NavigateToBookManagement()
        {
            if (!EnsureManagerAccess())
            {
                return;
            }

            CurrentManagerPageKey = "BookManagement";

            if (_bookManagementViewModel == null)
            {
                _bookManagementViewModel = new BookManagementViewModel(
                    _bookService,
                    _mockDataService,
                    _dialogService);
            }

            if (_managerFrame != null && _bookManagementPage == null)
            {
                _bookManagementPage = new BookManagementPage(_bookManagementViewModel);
            }

            _bookManagementViewModel.OnNavigatedTo(null);

            if (_bookManagementPage != null)
            {
                NavigateFrame(_managerFrame, _bookManagementPage);
            }

            ManagerNavigated?.Invoke(CurrentManagerPageKey);
        }

        public void NavigateToStaffManagement()
        {
            if (!EnsureManagerAccess())
            {
                return;
            }

            CurrentManagerPageKey = "StaffManagement";

            if (_staffManagementViewModel == null)
            {
                _staffManagementViewModel = new StaffManagementViewModel(
                    _userManagementService,
                    _authService,
                    _mockDataService,
                    _dialogService);
            }

            if (_managerFrame != null && _staffManagementPage == null)
            {
                _staffManagementPage = new StaffManagementPage(_staffManagementViewModel);
            }

            _staffManagementViewModel.OnNavigatedTo(null);

            if (_staffManagementPage != null)
            {
                NavigateFrame(_managerFrame, _staffManagementPage);
            }

            ManagerNavigated?.Invoke(CurrentManagerPageKey);
        }

        public void NavigateToCustomerManagement()
        {
            if (!EnsureManagerAccess())
            {
                return;
            }

            CurrentManagerPageKey = "CustomerManagement";

            if (_customerManagementViewModel == null)
            {
                _customerManagementViewModel = new CustomerManagementViewModel(
                    _userManagementService,
                    _authService,
                    _mockDataService,
                    _dialogService);
            }

            if (_managerFrame != null && _customerManagementPage == null)
            {
                _customerManagementPage = new CustomerManagementPage(_customerManagementViewModel);
            }

            _customerManagementViewModel.OnNavigatedTo(null);

            if (_customerManagementPage != null)
            {
                NavigateFrame(_managerFrame, _customerManagementPage);
            }

            ManagerNavigated?.Invoke(CurrentManagerPageKey);
        }

        public void NavigateToSupplierManagement()
        {
            if (!EnsureManagerAccess())
            {
                return;
            }

            CurrentManagerPageKey = "SupplierManagement";

            if (_supplierManagementViewModel == null)
            {
                _supplierManagementViewModel = new SupplierManagementViewModel(
                    _supplierService,
                    _mockDataService,
                    _dialogService);
            }

            if (_managerFrame != null && _supplierManagementPage == null)
            {
                _supplierManagementPage = new SupplierManagementPage(_supplierManagementViewModel);
            }

            _supplierManagementViewModel.OnNavigatedTo(null);

            if (_supplierManagementPage != null)
            {
                NavigateFrame(_managerFrame, _supplierManagementPage);
            }

            ManagerNavigated?.Invoke(CurrentManagerPageKey);
        }

        public void NavigateToImportReceipt()
        {
            if (!EnsureManagerAccess())
            {
                return;
            }

            CurrentManagerPageKey = "ImportReceipt";

            if (_importReceiptViewModel == null)
            {
                _importReceiptViewModel = new ImportReceiptViewModel(
                    _importService,
                    _supplierService,
                    _bookService,
                    _authService,
                    _mockDataService,
                    this,
                    _dialogService);
            }

            if (_managerFrame != null && _importReceiptPage == null)
            {
                _importReceiptPage = new ImportReceiptPage(_importReceiptViewModel);
            }

            _importReceiptViewModel.OnNavigatedTo(null);

            if (_importReceiptPage != null)
            {
                NavigateFrame(_managerFrame, _importReceiptPage);
            }

            ManagerNavigated?.Invoke(CurrentManagerPageKey);
        }

        public void NavigateToImportHistory()
        {
            if (!EnsureManagerAccess())
            {
                return;
            }

            CurrentManagerPageKey = "ImportHistory";

            if (_importHistoryViewModel == null)
            {
                _importHistoryViewModel = new ImportHistoryViewModel(
                    _importService,
                    _mockDataService,
                    this);
            }

            if (_managerFrame != null && _importHistoryPage == null)
            {
                _importHistoryPage = new ImportHistoryPage(_importHistoryViewModel);
            }

            _importHistoryViewModel.OnNavigatedTo(null);

            if (_importHistoryPage != null)
            {
                NavigateFrame(_managerFrame, _importHistoryPage);
            }

            ManagerNavigated?.Invoke(CurrentManagerPageKey);
        }

        // ==================== ROLE ROUTING & WINDOW LIFECYCLE ====================

        public void RouteAuthenticatedUser(User user)
        {
            if (user == null || !_authService.IsAuthenticated || _authService.CurrentUser == null)
            {
                _dialogService.ShowError("Xác thực không hợp lệ", "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.");
                LogoutToAuthWindow();
                return;
            }

            if (Application.Current == null)
            {
                return;
            }

            if (user.Role == UserRole.Customer)
            {
                var customerVm = new CustomerMainViewModel(_authService, _cartService, this, _dialogService);
                var customerWindow = new CustomerMainWindow(customerVm);
                SwitchMainWindow(customerWindow);
                Initialize(customerWindow.NavigationFrame);
            }
            else if (user.Role == UserRole.Staff)
            {
                var staffVm = new StaffMainViewModel(_authService, this, _dialogService);
                var staffWindow = new StaffMainWindow(staffVm);
                SwitchMainWindow(staffWindow);
                InitializeStaffFrame(staffWindow.NavigationFrame);
            }
            else if (user.Role == UserRole.Manager)
            {
                var managerVm = new ManagerMainViewModel(_authService, this, _dialogService);
                var managerWindow = new ManagerMainWindow(managerVm);
                SwitchMainWindow(managerWindow);
                InitializeManagerFrame(managerWindow.NavigationFrame);
            }
            else
            {
                _dialogService.ShowError("Vai trò không hợp lệ", "Tài khoản không có phân quyền hợp lệ.");
                LogoutToAuthWindow();
            }
        }

        public void LogoutToAuthWindow()
        {
            _authService.Logout();
            _customerFrame = null;
            _staffFrame = null;
            _managerFrame = null;

            if (Application.Current == null)
            {
                return;
            }

            var authVm = new AuthViewModel(_authService, this, _dialogService);
            var authWindow = new AuthWindow(authVm);
            SwitchMainWindow(authWindow);
        }

        private static void SwitchMainWindow(Window newWindow)
        {
            var app = Application.Current;
            if (app == null)
            {
                return;
            }

            var previousWindow = app.MainWindow;

            // Prevent application from terminating while replacing the active window
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            app.MainWindow = newWindow;
            newWindow.Show();

            if (previousWindow != null && !ReferenceEquals(previousWindow, newWindow))
            {
                previousWindow.Close();
            }

            // Restore normal shutdown behavior so closing the active window terminates the app
            app.ShutdownMode = ShutdownMode.OnMainWindowClose;
        }

        private bool EnsureCustomerAccess()
        {
            if (_authService.IsAuthenticated && !_authService.HasRole(UserRole.Customer))
            {
                _dialogService.ShowError(
                    "Không có quyền truy cập",
                    "Giao diện này chỉ dành cho tài khoản Khách hàng.");
                LogoutToAuthWindow();
                return false;
            }
            return true;
        }

        private bool EnsureStaffAccess()
        {
            if (!_authService.HasRole(UserRole.Staff))
            {
                _dialogService.ShowError(
                    "Không có quyền truy cập",
                    "Cổng Nhân viên yêu cầu phiên đăng nhập Nhân viên hợp lệ.");
                LogoutToAuthWindow();
                return false;
            }
            return true;
        }

        private bool EnsureManagerAccess()
        {
            if (!_authService.HasRole(UserRole.Manager))
            {
                _dialogService.ShowError(
                    "Không có quyền truy cập",
                    "Cổng Quản trị (Manager Portal) chỉ dành cho tài khoản Quản lý hợp lệ.");
                LogoutToAuthWindow();
                return false;
            }
            return true;
        }

        private static void NavigateFrame(Frame? frame, Page page)
        {
            if (frame == null)
            {
                return;
            }

            if (!ReferenceEquals(frame.Content, page))
            {
                frame.Navigate(page);
            }

            while (frame.CanGoBack)
            {
                frame.RemoveBackEntry();
            }
        }
    }
}
