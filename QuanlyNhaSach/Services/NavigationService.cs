using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
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

        private readonly AuthService _authService;
        private readonly CartService _cartService;
        private readonly OrderService _orderService;
        private readonly InventoryService _inventoryService;
        private readonly IDialogService _dialogService;
        private readonly BookService _bookService;
        private readonly UserManagementService _userManagementService;
        private readonly SupplierService _supplierService;
        private readonly ImportService _importService;
        private readonly EmailService _emailService;

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
            AuthService authService,
            CartService cartService,
            OrderService orderService,
            InventoryService inventoryService,
            IDialogService dialogService,
            BookService? bookService = null,
            UserManagementService? userManagementService = null,
            SupplierService? supplierService = null,
            ImportService? importService = null,
            EmailService? emailService = null)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _cartService = cartService ?? throw new ArgumentNullException(nameof(cartService));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _bookService = bookService ?? new BookService();
            _userManagementService = userManagementService ?? new UserManagementService();
            _supplierService = supplierService ?? new SupplierService();
            _importService = importService ?? new ImportService();
            _emailService = emailService ?? new EmailService();

            // Đồng bộ tự động làm mới Cache khi có thay đổi dữ liệu liên thông giữa các Service
            _importService.ImportReceiptConfirmed += (_, _) => _bookService.InvalidateCache();
            _inventoryService.InventoryChanged += (_, _) =>
            {
                _bookService.InvalidateCache();
                _orderService.InvalidateCache();
            };
            _supplierService.SuppliersChanged += (_, _) => _bookService.InvalidateCache();
            _orderService.OrderUpdated += (_, _) => _bookService.InvalidateCache();

            WarmUpCachesInBackground();
        }

        private void WarmUpCachesInBackground()
        {
            Task.Run(() =>
            {
                try
                {
                    _ = _bookService.GetAllBooks();
                    _ = _supplierService.GetAllSuppliers();
                    _ = _orderService.GetAllOrders();
                    _ = _importService.GetConfirmedReceipts();
                }
                catch
                {
                    // Bỏ qua nếu SQL Server chưa bật
                }
            });
        }

        private static void RunAfterFrameRender(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null)
            {
                dispatcher.InvokeAsync(action, DispatcherPriority.Background);
            }
            else
            {
                action();
            }
        }

        // ==================== CUSTOMER NAVIGATION ====================

        public void Initialize(Frame frame)
        {
            _customerFrame = frame ?? throw new ArgumentNullException(nameof(frame));
            NavigateToHome();
        }

        public void NavigateToHome(string? searchKeyword = null)
        {
            if (!EnsureCustomerAccess()) return;

            CurrentPageKey = "Home";
            _homeViewModel ??= new HomeViewModel(_bookService, _cartService, this, _dialogService);
            if (_customerFrame != null && _homePage == null)
            {
                _homePage = new HomePage(_homeViewModel);
            }

            if (_homePage != null)
            {
                NavigateFrame(_customerFrame, _homePage);
            }
            Navigated?.Invoke(CurrentPageKey);

            if (_homeViewModel is INavigationAware aware)
            {
                RunAfterFrameRender(() => aware.OnNavigatedTo(searchKeyword));
            }
        }

        public void NavigateToBookDetail(string maSach)
        {
            if (!EnsureCustomerAccess()) return;

            CurrentPageKey = "BookDetail";
            _bookDetailViewModel ??= new BookDetailViewModel(_bookService, _cartService, this, _dialogService);
            if (_customerFrame != null && _bookDetailPage == null)
            {
                _bookDetailPage = new BookDetailPage(_bookDetailViewModel);
            }

            if (_bookDetailPage != null)
            {
                NavigateFrame(_customerFrame, _bookDetailPage);
            }
            Navigated?.Invoke(CurrentPageKey);

            if (_bookDetailViewModel is INavigationAware aware)
            {
                RunAfterFrameRender(() => aware.OnNavigatedTo(maSach));
            }
        }

        public void NavigateToCartCheckout()
        {
            if (!EnsureCustomerAccess()) return;

            CurrentPageKey = "CartCheckout";
            _cartCheckoutViewModel ??= new CartCheckoutViewModel(_cartService, this, _dialogService, _authService, _orderService);
            if (_customerFrame != null && _cartCheckoutPage == null)
            {
                _cartCheckoutPage = new CartCheckoutPage(_cartCheckoutViewModel);
            }

            if (_cartCheckoutPage != null)
            {
                NavigateFrame(_customerFrame, _cartCheckoutPage);
            }
            Navigated?.Invoke(CurrentPageKey);

            if (_cartCheckoutViewModel is INavigationAware aware)
            {
                RunAfterFrameRender(() => aware.OnNavigatedTo(null));
            }
        }

        public void NavigateToCustomerProfile(string? tab = null)
        {
            if (!EnsureCustomerAccess()) return;

            CurrentPageKey = "CustomerProfile";
            _customerProfileViewModel ??= new CustomerProfileViewModel(this, _dialogService, _authService, _orderService);
            if (_customerFrame != null && _customerProfilePage == null)
            {
                _customerProfilePage = new CustomerProfilePage(_customerProfileViewModel);
            }

            if (_customerProfilePage != null)
            {
                NavigateFrame(_customerFrame, _customerProfilePage);
            }
            Navigated?.Invoke(CurrentPageKey);

            if (_customerProfileViewModel is INavigationAware aware)
            {
                RunAfterFrameRender(() => aware.OnNavigatedTo(tab));
            }
        }

        // ==================== STAFF NAVIGATION ====================

        public void InitializeStaffFrame(Frame frame)
        {
            _staffFrame = frame ?? throw new ArgumentNullException(nameof(frame));
            NavigateToStaffDashboard();
        }

        public void NavigateToStaffDashboard()
        {
            if (!EnsureStaffAccess()) return;

            CurrentStaffPageKey = "Dashboard";
            _staffDashboardViewModel ??= new StaffDashboardViewModel(_orderService, this);
            if (_staffFrame != null && _staffDashboardPage == null)
            {
                _staffDashboardPage = new StaffDashboardPage(_staffDashboardViewModel);
            }

            if (_staffDashboardPage != null)
            {
                NavigateFrame(_staffFrame, _staffDashboardPage);
            }
            StaffNavigated?.Invoke(CurrentStaffPageKey);

            var vm = _staffDashboardViewModel;
            RunAfterFrameRender(() => vm.OnNavigatedTo(null));
        }

        public void NavigateToStaffLookup(string? initialKeyword = null)
        {
            if (!EnsureStaffAccess()) return;

            CurrentStaffPageKey = "Lookup";
            _staffLookupViewModel ??= new StaffLookupViewModel(_bookService, this, _orderService);
            if (_staffFrame != null && _staffLookupPage == null)
            {
                _staffLookupPage = new StaffLookupPage(_staffLookupViewModel);
            }

            if (_staffLookupPage != null)
            {
                NavigateFrame(_staffFrame, _staffLookupPage);
            }
            StaffNavigated?.Invoke(CurrentStaffPageKey);

            var vm = _staffLookupViewModel;
            RunAfterFrameRender(() => vm.OnNavigatedTo(initialKeyword));
        }

        public void NavigateToStaffSales(int? orderId = null)
        {
            if (!EnsureStaffAccess()) return;

            CurrentStaffPageKey = "Sales";
            _staffSalesViewModel ??= new StaffSalesViewModel(_orderService, this, _dialogService);
            if (_staffFrame != null && _staffSalesPage == null)
            {
                _staffSalesPage = new StaffSalesPage(_staffSalesViewModel);
            }

            if (_staffSalesPage != null)
            {
                NavigateFrame(_staffFrame, _staffSalesPage);
            }
            StaffNavigated?.Invoke(CurrentStaffPageKey);

            var vm = _staffSalesViewModel;
            RunAfterFrameRender(() => vm.OnNavigatedTo(orderId));
        }

        public void NavigateToStaffOrderProcessing(int? orderId = null)
        {
            if (!EnsureStaffAccess()) return;

            CurrentStaffPageKey = "OrderProcessing";
            _staffOrderProcessingViewModel ??= new StaffOrderProcessingViewModel(_orderService, this, _dialogService);
            if (_staffFrame != null && _staffOrderProcessingPage == null)
            {
                _staffOrderProcessingPage = new StaffOrderProcessingPage(_staffOrderProcessingViewModel);
            }

            if (_staffOrderProcessingPage != null)
            {
                NavigateFrame(_staffFrame, _staffOrderProcessingPage);
            }
            StaffNavigated?.Invoke(CurrentStaffPageKey);

            var vm = _staffOrderProcessingViewModel;
            RunAfterFrameRender(() => vm.OnNavigatedTo(orderId));
        }

        public void NavigateToStaffStockOut(int? orderId = null)
        {
            if (!EnsureStaffAccess()) return;

            CurrentStaffPageKey = "StockOut";
            _staffStockOutViewModel ??= new StaffStockOutViewModel(_orderService, _inventoryService, this, _dialogService);
            if (_staffFrame != null && _staffStockOutPage == null)
            {
                _staffStockOutPage = new StaffStockOutPage(_staffStockOutViewModel);
            }

            if (_staffStockOutPage != null)
            {
                NavigateFrame(_staffFrame, _staffStockOutPage);
            }
            StaffNavigated?.Invoke(CurrentStaffPageKey);

            var vm = _staffStockOutViewModel;
            RunAfterFrameRender(() => vm.OnNavigatedTo(orderId));
        }

        // ==================== MANAGER NAVIGATION (7 ROUTES) ====================

        public void InitializeManagerFrame(Frame frame)
        {
            _managerFrame = frame ?? throw new ArgumentNullException(nameof(frame));
            NavigateToManagerOverview();
        }

        public void NavigateToManagerOverview()
        {
            if (!EnsureManagerAccess()) return;

            CurrentManagerPageKey = "Overview";
            _managerOverviewViewModel ??= new ManagerOverviewViewModel(_orderService, _bookService, this);
            if (_managerFrame != null && _managerOverviewPage == null)
            {
                _managerOverviewPage = new ManagerOverviewPage(_managerOverviewViewModel);
            }

            if (_managerOverviewPage != null)
            {
                NavigateFrame(_managerFrame, _managerOverviewPage);
            }
            ManagerNavigated?.Invoke(CurrentManagerPageKey);

            var vm = _managerOverviewViewModel;
            RunAfterFrameRender(() => vm.OnNavigatedTo(null));
        }

        public void NavigateToBookManagement()
        {
            if (!EnsureManagerAccess()) return;

            CurrentManagerPageKey = "BookManagement";
            _bookManagementViewModel ??= new BookManagementViewModel(_bookService, _dialogService);
            if (_managerFrame != null && _bookManagementPage == null)
            {
                _bookManagementPage = new BookManagementPage(_bookManagementViewModel);
            }

            if (_bookManagementPage != null)
            {
                NavigateFrame(_managerFrame, _bookManagementPage);
            }
            ManagerNavigated?.Invoke(CurrentManagerPageKey);

            var vm = _bookManagementViewModel;
            RunAfterFrameRender(() => vm.OnNavigatedTo(null));
        }

        public void NavigateToStaffManagement()
        {
            if (!EnsureManagerAccess()) return;

            CurrentManagerPageKey = "StaffManagement";
            _staffManagementViewModel ??= new StaffManagementViewModel(_userManagementService, _authService, _dialogService, _emailService);
            if (_managerFrame != null && _staffManagementPage == null)
            {
                _staffManagementPage = new StaffManagementPage(_staffManagementViewModel);
            }

            if (_staffManagementPage != null)
            {
                NavigateFrame(_managerFrame, _staffManagementPage);
            }
            ManagerNavigated?.Invoke(CurrentManagerPageKey);

            var vm = _staffManagementViewModel;
            RunAfterFrameRender(() => vm.OnNavigatedTo(null));
        }

        public void NavigateToCustomerManagement()
        {
            if (!EnsureManagerAccess()) return;

            CurrentManagerPageKey = "CustomerManagement";
            _customerManagementViewModel ??= new CustomerManagementViewModel(_userManagementService, _authService, _dialogService);
            if (_managerFrame != null && _customerManagementPage == null)
            {
                _customerManagementPage = new CustomerManagementPage(_customerManagementViewModel);
            }

            if (_customerManagementPage != null)
            {
                NavigateFrame(_managerFrame, _customerManagementPage);
            }
            ManagerNavigated?.Invoke(CurrentManagerPageKey);

            var vm = _customerManagementViewModel;
            RunAfterFrameRender(() => vm.OnNavigatedTo(null));
        }

        public void NavigateToSupplierManagement()
        {
            if (!EnsureManagerAccess()) return;

            CurrentManagerPageKey = "SupplierManagement";
            _supplierManagementViewModel ??= new SupplierManagementViewModel(_supplierService, _dialogService);
            if (_managerFrame != null && _supplierManagementPage == null)
            {
                _supplierManagementPage = new SupplierManagementPage(_supplierManagementViewModel);
            }

            if (_supplierManagementPage != null)
            {
                NavigateFrame(_managerFrame, _supplierManagementPage);
            }
            ManagerNavigated?.Invoke(CurrentManagerPageKey);

            var vm = _supplierManagementViewModel;
            RunAfterFrameRender(() => vm.OnNavigatedTo(null));
        }

        public void NavigateToImportReceipt()
        {
            if (!EnsureManagerAccess()) return;

            CurrentManagerPageKey = "ImportReceipt";
            _importReceiptViewModel ??= new ImportReceiptViewModel(_importService, _supplierService, _bookService, _authService, this, _dialogService);
            if (_managerFrame != null && _importReceiptPage == null)
            {
                _importReceiptPage = new ImportReceiptPage(_importReceiptViewModel);
            }

            if (_importReceiptPage != null)
            {
                NavigateFrame(_managerFrame, _importReceiptPage);
            }
            ManagerNavigated?.Invoke(CurrentManagerPageKey);

            var vm = _importReceiptViewModel;
            RunAfterFrameRender(() => vm.OnNavigatedTo(null));
        }

        public void NavigateToImportHistory()
        {
            if (!EnsureManagerAccess()) return;

            CurrentManagerPageKey = "ImportHistory";
            _importHistoryViewModel ??= new ImportHistoryViewModel(_importService, this);
            if (_managerFrame != null && _importHistoryPage == null)
            {
                _importHistoryPage = new ImportHistoryPage(_importHistoryViewModel);
            }

            if (_importHistoryPage != null)
            {
                NavigateFrame(_managerFrame, _importHistoryPage);
            }
            ManagerNavigated?.Invoke(CurrentManagerPageKey);

            var vm = _importHistoryViewModel;
            RunAfterFrameRender(() => vm.OnNavigatedTo(null));
        }

        // ==================== ROLE ROUTING & WINDOW LIFECYCLE ====================

        public void RouteAuthenticatedUser(NGUOIDUNG user)
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

            WarmUpCachesInBackground();

            if (user.Role == UserRole.Customer)
            {
                var customerVm = new CustomerMainViewModel(_authService, _cartService, this, _dialogService, _bookService);
                var customerWindow = new CustomerMainWindow(customerVm);
                SwitchMainWindow(customerWindow, maximize: true);
                Initialize(customerWindow.NavigationFrame);
            }
            else if (user.Role == UserRole.Staff)
            {
                var staffVm = new StaffMainViewModel(_authService, this, _dialogService);
                var staffWindow = new StaffMainWindow(staffVm);
                SwitchMainWindow(staffWindow, maximize: true);
                InitializeStaffFrame(staffWindow.NavigationFrame);
            }
            else if (user.Role == UserRole.Manager)
            {
                var managerVm = new ManagerMainViewModel(_authService, this, _dialogService);
                var managerWindow = new ManagerMainWindow(managerVm);
                SwitchMainWindow(managerWindow, maximize: true);
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

            _customerProfilePage = null;
            _customerProfileViewModel = null;
            _cartCheckoutPage = null;
            _cartCheckoutViewModel = null;

            if (Application.Current == null)
            {
                return;
            }

            var authVm = new AuthViewModel(_authService, this, _dialogService, _emailService);
            var authWindow = new AuthWindow(authVm);
            SwitchMainWindow(authWindow, maximize: false);
        }

        private static void SwitchMainWindow(Window newWindow, bool maximize = false)
        {
            var app = Application.Current;
            if (app == null)
            {
                return;
            }

            var previousWindow = app.MainWindow;

            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            if (maximize)
            {
                newWindow.WindowState = WindowState.Maximized;
            }
            app.MainWindow = newWindow;
            newWindow.Show();

            if (previousWindow != null && !ReferenceEquals(previousWindow, newWindow))
            {
                previousWindow.Close();
            }

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
                    "Cổng Quản trị chỉ dành cho tài khoản Quản lý.");
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
