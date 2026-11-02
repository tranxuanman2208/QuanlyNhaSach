using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanlyNhaSach.Models;
using QuanlyNhaSach.Navigation;
using QuanlyNhaSach.Services;

namespace QuanlyNhaSach.ViewModels.Staff
{
    public partial class StaffLookupViewModel : ObservableObject, INavigationAware
    {
        public const string AllScope = "Tất cả (Đơn hàng & Sách)";
        public const string OrdersOnlyScope = "Chỉ Đơn hàng & Khách hàng";
        public const string BooksOnlyScope = "Chỉ Danh mục Sách & Tồn kho";

        private readonly MockDataService _mockDataService;
        private readonly OrderService _orderService;
        private readonly INavigationService _navigationService;

        public ObservableCollection<Order> MatchedOrders { get; } = new();
        public ObservableCollection<Book> MatchedBooks { get; } = new();

        public IReadOnlyList<string> SearchScopes { get; } = new List<string>
        {
            AllScope,
            OrdersOnlyScope,
            BooksOnlyScope
        };

        [ObservableProperty]
        private string _searchKeyword = string.Empty;

        [ObservableProperty]
        private string _selectedScope = AllScope;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowOrdersSection))]
        [NotifyPropertyChangedFor(nameof(IsNoResults))]
        private bool _hasMatchedOrders;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowBooksSection))]
        [NotifyPropertyChangedFor(nameof(IsNoResults))]
        private bool _hasMatchedBooks;

        [ObservableProperty]
        private string _resultSummary = string.Empty;

        public bool ShowOrdersSection =>
            HasMatchedOrders && (SelectedScope == AllScope || SelectedScope == OrdersOnlyScope);

        public bool ShowBooksSection =>
            HasMatchedBooks && (SelectedScope == AllScope || SelectedScope == BooksOnlyScope);

        public bool IsNoResults => !ShowOrdersSection && !ShowBooksSection;

        public StaffLookupViewModel(
            MockDataService mockDataService,
            OrderService orderService,
            INavigationService navigationService)
        {
            _mockDataService = mockDataService ?? throw new ArgumentNullException(nameof(mockDataService));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            _mockDataService.DataChanged += (_, _) => ExecuteLookup();
            ExecuteLookup();
        }

        partial void OnSelectedScopeChanged(string value)
        {
            ExecuteLookup();
        }

        public void OnNavigatedTo(object? parameter)
        {
            if (parameter is string keyword && !string.IsNullOrWhiteSpace(keyword))
            {
                SearchKeyword = keyword;
            }
            ExecuteLookup();
        }

        [RelayCommand]
        public void ExecuteLookup()
        {
            string normalized = SearchKeyword?.Trim() ?? string.Empty;

            MatchedOrders.Clear();
            if (SelectedScope == AllScope || SelectedScope == OrdersOnlyScope)
            {
                var orders = _orderService.SearchOrders(normalized);
                foreach (var order in orders)
                {
                    MatchedOrders.Add(order);
                }
            }

            MatchedBooks.Clear();
            if (SelectedScope == AllScope || SelectedScope == BooksOnlyScope)
            {
                var books = _mockDataService.SearchBooks(normalized);
                foreach (var book in books)
                {
                    MatchedBooks.Add(book);
                }
            }

            HasMatchedOrders = MatchedOrders.Count > 0;
            HasMatchedBooks = MatchedBooks.Count > 0;
            OnPropertyChanged(nameof(ShowOrdersSection));
            OnPropertyChanged(nameof(ShowBooksSection));
            OnPropertyChanged(nameof(IsNoResults));

            if (string.IsNullOrWhiteSpace(normalized))
            {
                ResultSummary = $"Hiển thị toàn bộ dữ liệu: {MatchedOrders.Count} đơn hàng và {MatchedBooks.Count} đầu sách.";
            }
            else
            {
                ResultSummary = $"Kết quả cho từ khóa \"{normalized}\": {MatchedOrders.Count} đơn hàng, {MatchedBooks.Count} đầu sách.";
            }
        }

        [RelayCommand]
        private void ClearLookup()
        {
            SearchKeyword = string.Empty;
            SelectedScope = AllScope;
            ExecuteLookup();
        }

        [RelayCommand]
        private void OpenOrderProcessing(Order? order)
        {
            if (order == null)
            {
                return;
            }
            _navigationService.NavigateToStaffOrderProcessing(order.MaDH);
        }

        [RelayCommand]
        private void OpenSales(Order? order)
        {
            if (order == null)
            {
                return;
            }
            _navigationService.NavigateToStaffSales(order.MaDH);
        }

        [RelayCommand]
        private void OpenStockOut(Order? order)
        {
            if (order == null)
            {
                return;
            }
            _navigationService.NavigateToStaffStockOut(order.MaDH);
        }
    }
}
