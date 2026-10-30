using System;
using System.Windows.Controls;
using QuanlyNhaSach.Models;

namespace QuanlyNhaSach.Services
{
    public interface INavigationService
    {
        string CurrentPageKey { get; }
        string CurrentStaffPageKey { get; }
        string CurrentManagerPageKey { get; }

        event Action<string>? Navigated;
        event Action<string>? StaffNavigated;
        event Action<string>? ManagerNavigated;

        // Customer Frame Navigation
        void Initialize(Frame frame);
        void NavigateToHome(string? searchKeyword = null);
        void NavigateToBookDetail(int maSach);
        void NavigateToCartCheckout();

        // Staff Frame Navigation
        void InitializeStaffFrame(Frame frame);
        void NavigateToStaffDashboard();
        void NavigateToStaffLookup(string? initialKeyword = null);
        void NavigateToStaffSales(int? orderId = null);
        void NavigateToStaffOrderProcessing(int? orderId = null);
        void NavigateToStaffStockOut(int? orderId = null);

        // Manager Frame Navigation
        void InitializeManagerFrame(Frame frame);
        void NavigateToManagerOverview();
        void NavigateToBookManagement();
        void NavigateToStaffManagement();
        void NavigateToCustomerManagement();
        void NavigateToSupplierManagement();
        void NavigateToImportReceipt();
        void NavigateToImportHistory();

        // Role Window & Session Routing
        void RouteAuthenticatedUser(User user);
        void LogoutToAuthWindow();
    }
}
