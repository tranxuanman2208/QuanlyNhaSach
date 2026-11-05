using System.Windows;
using QuanlyNhaSach.Services;
using QuanlyNhaSach.ViewModels;
using QuanlyNhaSach.Views.Auth;
using QuanlyNhaSach.Models;

namespace QuanlyNhaSach
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
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

            // 1. Instantiate shared application-level singleton services (SQL Server EF Core Integrated)
            AuthService = new AuthService();
            BookService = new BookService();
            UserManagementService = new UserManagementService();
            SupplierService = new SupplierService();
            ImportService = new ImportService();
            CartService = new CartService(BookService);
            OrderService = new OrderService();
            InventoryService = new InventoryService();
            DialogService = new DialogService();
            NavigationService = new NavigationService(
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
