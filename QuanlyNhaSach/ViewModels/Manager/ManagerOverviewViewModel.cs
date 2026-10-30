using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanlyNhaSach.Models;
using QuanlyNhaSach.Models.Enums;
using QuanlyNhaSach.Navigation;
using QuanlyNhaSach.Services;

namespace QuanlyNhaSach.ViewModels.Manager
{
    public partial class ManagerOverviewViewModel : ObservableObject, INavigationAware
    {
        public const string Period7Days = "7 ngày gần nhất";
        public const string Period14Days = "14 ngày gần nhất";

        private readonly MockDataStore _dataStore;
        private readonly OrderService _orderService;
        private readonly BookService _bookService;
        private readonly INavigationService _navigationService;

        public ObservableCollection<RevenueChartPoint> RevenueChartPoints { get; } = new();
        public ObservableCollection<BestSellingBookItem> TopFiveBooks { get; } = new();
        public ObservableCollection<Book> LowStockBooks { get; } = new();

        public IReadOnlyList<string> ReportingPeriods { get; } = new List<string>
        {
            Period7Days,
            Period14Days
        };

        [ObservableProperty]
        private string _selectedPeriod = Period7Days;

        // Card 1: Tổng doanh thu
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FormattedTotalRevenue))]
        private decimal _totalRevenue;

        public string FormattedTotalRevenue => $"{TotalRevenue:N0} ₫";

        [ObservableProperty]
        private string _revenueSubtext = string.Empty;

        // Card 2: Tổng đơn hàng
        [ObservableProperty]
        private int _totalOrders;

        [ObservableProperty]
        private string _ordersSubtext = string.Empty;

        // Card 3: Sách bán chạy
        [ObservableProperty]
        private string _bestSellingBookTitle = "Chưa có dữ liệu";

        [ObservableProperty]
        private string _bestSellingBookSubtext = "0 cuốn đã bán";

        // Card 4: Sách sắp hết kho (< 10)
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasLowStockBooks))]
        private int _lowStockBookCount;

        [ObservableProperty]
        private string _lowStockSubtext = string.Empty;

        [ObservableProperty]
        private string _chartPeriodTotalText = "0 ₫";

        public bool HasLowStockBooks => LowStockBookCount > 0;
        public bool HasTopBooks => TopFiveBooks.Count > 0;

        public ManagerOverviewViewModel(
            MockDataStore dataStore,
            OrderService orderService,
            BookService bookService,
            INavigationService navigationService)
        {
            _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _bookService = bookService ?? throw new ArgumentNullException(nameof(bookService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            _dataStore.DataChanged += OnSharedDataChanged;
            _dataStore.Orders.CollectionChanged += OnCollectionChanged;
            _dataStore.Books.CollectionChanged += OnCollectionChanged;
            _orderService.OrderUpdated += OnOrderUpdated;

            RefreshOverview();
        }

        partial void OnSelectedPeriodChanged(string value)
        {
            RefreshOverview();
        }

        public void OnNavigatedTo(object? parameter)
        {
            RefreshOverview();
        }

        private void OnSharedDataChanged(object? sender, EventArgs e)
        {
            RefreshOverview();
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshOverview();
        }

        private void OnOrderUpdated(object? sender, Order? e)
        {
            RefreshOverview();
        }

        [RelayCommand]
        public void RefreshOverview()
        {
            // 1. Card 1: Total Realized Revenue (Completed & Paid orders only)
            var eligiblePaidOrders = _orderService.GetEligiblePaidCompletedOrders();
            TotalRevenue = eligiblePaidOrders.Sum(o => o.TongTien);
            RevenueSubtext = $"Từ {eligiblePaidOrders.Count} đơn hàng hoàn tất & đã thu tiền";

            // 2. Card 2: Total Orders
            TotalOrders = _orderService.GetTotalOrdersCount();
            int completedCount = _dataStore.Orders.Count(o => o.Status == OrderStatus.Completed);
            int cancelledCount = _dataStore.Orders.Count(o => o.Status == OrderStatus.Cancelled);
            int processingCount = Math.Max(0, TotalOrders - completedCount - cancelledCount);
            OrdersSubtext = $"{completedCount} hoàn tất • {processingCount} đang xử lý • {cancelledCount} đã hủy";

            // 3. Card 3 & Top 5 Best-Selling Books
            var topBooks = _orderService.GetTopSellingBooks(5);
            TopFiveBooks.Clear();
            foreach (var item in topBooks)
            {
                TopFiveBooks.Add(item);
            }
            OnPropertyChanged(nameof(HasTopBooks));

            var bestBook = topBooks.FirstOrDefault();
            if (bestBook != null)
            {
                BestSellingBookTitle = bestBook.TenSach;
                BestSellingBookSubtext = $"Đã bán {bestBook.QuantitySold} cuốn • Doanh thu {bestBook.FormattedRevenue}";
            }
            else
            {
                BestSellingBookTitle = "Chưa phát sinh";
                BestSellingBookSubtext = "0 cuốn đã bán";
            }

            // 4. Card 4 & Low-Stock Books (SoLuongTon < 10)
            var lowStockList = _bookService.GetLowStockBooks();
            LowStockBooks.Clear();
            foreach (var book in lowStockList)
            {
                LowStockBooks.Add(book);
            }

            LowStockBookCount = lowStockList.Count;
            int outOfStockCount = lowStockList.Count(b => b.SoLuongTon <= 0);
            int warningCount = lowStockList.Count(b => b.SoLuongTon > 0 && b.SoLuongTon < 10);
            LowStockSubtext = $"{outOfStockCount} hết hàng (0 cuốn) • {warningCount} sắp hết (1–9 cuốn)";

            // 5. Revenue Bar Chart
            int days = SelectedPeriod == Period14Days ? 14 : 7;
            var points = _orderService.GetRevenueChartPoints(days, maxBarPixelHeight: 170.0);
            RevenueChartPoints.Clear();
            foreach (var pt in points)
            {
                RevenueChartPoints.Add(pt);
            }

            decimal periodSum = points.Sum(p => p.Revenue);
            ChartPeriodTotalText = $"Tổng doanh thu kỳ hiển thị ({days} ngày): {periodSum:N0} ₫";
        }

        [RelayCommand]
        private void GoToBookManagement()
        {
            _navigationService.NavigateToBookManagement();
        }

        [RelayCommand]
        private void GoToImportReceipt()
        {
            _navigationService.NavigateToImportReceipt();
        }
    }
}
