using System;
using System.IO;
using System.Linq;
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
        public static EmailService EmailService { get; private set; } = null!;
        public static INavigationService NavigationService { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (e.Args.Any(a => string.Equals(a, "--init-db", StringComparison.OrdinalIgnoreCase)))
            {
                string statusFile = Path.Combine(AppContext.BaseDirectory, "init_db_status.txt");
                try
                {
                    using var db = new QuanlyNhaSachContext();
                    int users = db.NGUOIDUNGs.Count();
                    int books = db.SACHs.Count();
                    int orders = db.DONHANGs.Count();
                    int suppliers = db.NHACUNGCAPs.Count();
                    int imports = db.PHIEUNHAPs.Count();
                    string msg = $"INIT_DB_SUCCESS: Users={users}, Books={books}, Orders={orders}, Suppliers={suppliers}, Imports={imports}";
                    File.WriteAllText(statusFile, msg);
                    Console.WriteLine(msg);
                    Shutdown(0);
                    return;
                }
                catch (Exception ex)
                {
                    string err = $"INIT_DB_ERROR: {ex}";
                    File.WriteAllText(statusFile, err);
                    Console.Error.WriteLine(err);
                    Shutdown(1);
                    return;
                }
            }

            // 1. Instantiate shared application-level singleton services (Cloud PostgreSQL EF Core Integrated)
            AuthService = new AuthService();
            BookService = new BookService();
            UserManagementService = new UserManagementService();
            SupplierService = new SupplierService();
            ImportService = new ImportService();
            CartService = new CartService(BookService);
            OrderService = new OrderService();
            InventoryService = new InventoryService();
            DialogService = new DialogService();
            EmailService = new EmailService();
            NavigationService = new NavigationService(
                AuthService,
                CartService,
                OrderService,
                InventoryService,
                DialogService,
                BookService,
                UserManagementService,
                SupplierService,
                ImportService,
                EmailService);

            // 2. Configure window lifecycle and start at AuthWindow (UC01 / UC14)
            ShutdownMode = ShutdownMode.OnMainWindowClose;

            var authViewModel = new AuthViewModel(AuthService, NavigationService, DialogService, EmailService);
            var authWindow = new AuthWindow(authViewModel);
            MainWindow = authWindow;
            authWindow.Show();
        }
    }
}
