using System.Windows;
using QuanlyNhaSach.Services;
using QuanlyNhaSach.ViewModels;
using QuanlyNhaSach.Views.Auth;

namespace QuanlyNhaSach
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static MockDataStore MockDataStore { get; private set; } = null!;
        public static MockDataService MockDataService { get; private set; } = null!;
        public static AuthService AuthService { get; private set; } = null!;
        public static BookService BookService { get; private set; } = null!;
        public static UserManagementService UserManagementService { get; private set; } = null!;
        public static SupplierService SupplierService { get; private set; } = null!;
        public static ImportService ImportService { get; private set; } = null!;
        public static CartService CartService { get; private set; } = null!;
        public static OrderService OrderService { get; private set; } = null!;
        public static InventoryService InventoryService { get; private set; } = null!;
        public static IDialogService DialogService { get; private set; } = null!;
        public static INavigationService NavigationService { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1. Instantiate shared application-level singleton services (100% In-Memory Mock State)
            var sharedDataInstance = new MockDataService();
            MockDataStore = sharedDataInstance;
            MockDataService = sharedDataInstance;

            AuthService = new AuthService(MockDataService);
            BookService = new BookService(MockDataStore);
            UserManagementService = new UserManagementService(MockDataStore);
            SupplierService = new SupplierService(MockDataStore);
            ImportService = new ImportService(MockDataStore);
            CartService = new CartService(MockDataService);
            OrderService = new OrderService(MockDataService);
            InventoryService = new InventoryService(MockDataService);
            DialogService = new DialogService();
            NavigationService = new NavigationService(
                MockDataService,
                AuthService,
                CartService,
                OrderService,
                InventoryService,
                DialogService,
                BookService,
                UserManagementService,
                SupplierService,
                ImportService);

            // 2. Configure window lifecycle and start at AuthWindow (UC01 / UC14)
            ShutdownMode = ShutdownMode.OnMainWindowClose;

            var authViewModel = new AuthViewModel(AuthService, NavigationService, DialogService);
            var authWindow = new AuthWindow(authViewModel);
            MainWindow = authWindow;
            authWindow.Show();
        }
    }
}
