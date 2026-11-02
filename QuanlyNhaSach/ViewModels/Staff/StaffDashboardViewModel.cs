using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanlyNhaSach.Models;
using QuanlyNhaSach.Navigation;
using QuanlyNhaSach.Services;

namespace QuanlyNhaSach.ViewModels.Staff
{
    public partial class StaffDashboardViewModel : ObservableObject, INavigationAware
    {
        private readonly MockDataService _mockDataService;
        private readonly OrderService _orderService;
        private readonly INavigationService _navigationService;

        public ObservableCollection<Order> UrgentOrders { get; } = new();

        [ObservableProperty]
        private int _newOrdersCount;

        [ObservableProperty]
        private int _confirmedOrdersCount;

        [ObservableProperty]
        private int _shippingOrdersCount;

        [ObservableProperty]
        private int _completedOrdersCount;

        [ObservableProperty]
        private int _cancelledOrdersCount;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasUrgentOrders))]
        [NotifyPropertyChangedFor(nameof(IsUrgentOrdersEmpty))]
        private int _urgentOrdersCount;

        [ObservableProperty]
        private string _formattedCompletedRevenue = "0 ₫";

        [ObservableProperty]
        private int _lowStockBooksCount;

        public bool HasUrgentOrders => UrgentOrdersCount > 0;
        public bool IsUrgentOrdersEmpty => UrgentOrdersCount == 0;

        public StaffDashboardViewModel(
            MockDataService mockDataService,
            OrderService orderService,
            INavigationService navigationService)
        {
            _mockDataService = mockDataService ?? throw new ArgumentNullException(nameof(mockDataService));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            _mockDataService.Orders.CollectionChanged += OnOrdersCollectionChanged;
            _mockDataService.DataChanged += OnSharedDataChanged;
            _orderService.OrderUpdated += OnOrderUpdated;

            RefreshDashboard();
        }

        public void OnNavigatedTo(object? parameter)
        {
            RefreshDashboard();
        }

        private void OnOrdersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshDashboard();
        }

        private void OnSharedDataChanged(object? sender, EventArgs e)
        {
            RefreshDashboard();
        }

        private void OnOrderUpdated(object? sender, Order? e)
        {
            RefreshDashboard();
        }

        [RelayCommand]
        public void RefreshDashboard()
        {
            var stats = _orderService.GetDashboardStatistics();
            NewOrdersCount = stats.NewOrdersCount;
            ConfirmedOrdersCount = stats.ConfirmedOrdersCount;
            ShippingOrdersCount = stats.ShippingOrdersCount;
            CompletedOrdersCount = stats.CompletedOrdersCount;
            CancelledOrdersCount = stats.CancelledOrdersCount;
            FormattedCompletedRevenue = stats.FormattedCompletedRevenue;
            LowStockBooksCount = stats.LowStockBooksCount;

            var urgentList = _orderService.GetUrgentOrders();
            UrgentOrders.Clear();
            foreach (var order in urgentList)
            {
                UrgentOrders.Add(order);
            }

            UrgentOrdersCount = UrgentOrders.Count;
        }

        [RelayCommand]
        private void ProcessOrder(Order? order)
        {
            if (order == null)
            {
                return;
            }
            _navigationService.NavigateToStaffOrderProcessing(order.MaDH);
        }

        [RelayCommand]
        private void GoToStockOut(Order? order)
        {
            if (order == null)
            {
                return;
            }
            _navigationService.NavigateToStaffStockOut(order.MaDH);
        }

        [RelayCommand]
        private void GoToSales(Order? order)
        {
            if (order == null)
            {
                return;
            }
            _navigationService.NavigateToStaffSales(order.MaDH);
        }
    }
}
