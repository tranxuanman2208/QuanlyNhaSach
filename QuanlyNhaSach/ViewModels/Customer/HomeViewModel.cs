using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanlyNhaSach.Models;
using QuanlyNhaSach.Navigation;
using QuanlyNhaSach.Services;

namespace QuanlyNhaSach.ViewModels.Customer
{
    public partial class HomeViewModel : ObservableObject, INavigationAware
    {
        public const string DefaultStockFilter = "Tất cả tình trạng";
        public const string InStockFilter = "Còn hàng";
        public const string OutOfStockFilter = "Hết hàng";

        public const string DefaultSortOption = "Sắp xếp: Mới nhất";
        public const string PriceLowToHighSort = "Giá: Thấp đến Cao";
        public const string PriceHighToLowSort = "Giá: Cao đến Thấp";

        private readonly MockDataService _mockDataService;
        private readonly CartService _cartService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;

        public ObservableCollection<Book> DisplayedBooks { get; } = new();

        public IReadOnlyList<string> StockFilterOptions { get; } = new List<string>
        {
            DefaultStockFilter,
            InStockFilter,
            OutOfStockFilter
        };

        public IReadOnlyList<string> SortOptions { get; } = new List<string>
        {
            DefaultSortOption,
            PriceLowToHighSort,
            PriceHighToLowSort
        };

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsSearchFiltered))]
        private string _activeKeyword = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsSearchFiltered))]
        private string _selectedStockFilter = DefaultStockFilter;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsSearchFiltered))]
        private string _selectedSortOption = DefaultSortOption;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEmptyResult))]
        private bool _hasBooks = true;

        [ObservableProperty]
        private int _totalCount;

        [ObservableProperty]
        private string _emptyStateMessage = string.Empty;

        [ObservableProperty]
        private string _statusBannerMessage = string.Empty;

        [ObservableProperty]
        private bool _isStatusBannerVisible;

        public bool IsEmptyResult => !HasBooks;

        public bool IsSearchFiltered =>
            !string.IsNullOrWhiteSpace(ActiveKeyword) ||
            SelectedStockFilter != DefaultStockFilter ||
            SelectedSortOption != DefaultSortOption;

        public HomeViewModel(
            MockDataService mockDataService,
            CartService cartService,
            INavigationService navigationService,
            IDialogService dialogService)
        {
            _mockDataService = mockDataService ?? throw new ArgumentNullException(nameof(mockDataService));
            _cartService = cartService ?? throw new ArgumentNullException(nameof(cartService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            _mockDataService.DataChanged += OnSharedDataChanged;
            ApplySearch(null);
        }

        private void OnSharedDataChanged(object? sender, EventArgs e)
        {
            ApplyFiltersAndSort();
        }

        partial void OnSelectedStockFilterChanged(string value)
        {
            ApplyFiltersAndSort();
        }

        partial void OnSelectedSortOptionChanged(string value)
        {
            ApplyFiltersAndSort();
        }

        public void OnNavigatedTo(object? parameter)
        {
            if (parameter is string keyword)
            {
                ApplySearch(keyword);
            }
            else if (parameter == null)
            {
                ApplySearch(ActiveKeyword);
            }
        }

        public void ApplySearch(string? keyword)
        {
            ActiveKeyword = keyword?.Trim() ?? string.Empty;
            ApplyFiltersAndSort();
        }

        public void ApplyFiltersAndSort()
        {
            IEnumerable<Book> query = _mockDataService.SearchBooks(ActiveKeyword);

            // 1. Filter by Stock Status
            if (string.Equals(SelectedStockFilter, InStockFilter, StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(b => b.SoLuongTon > 0);
            }
            else if (string.Equals(SelectedStockFilter, OutOfStockFilter, StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(b => b.SoLuongTon <= 0);
            }

            // 2. Sort by Selected Option
            query = SelectedSortOption switch
            {
                PriceLowToHighSort => query.OrderBy(b => b.GiaBan).ThenBy(b => b.MaSach),
                PriceHighToLowSort => query.OrderByDescending(b => b.GiaBan).ThenBy(b => b.MaSach),
                _ => query
            };

            DisplayedBooks.Clear();
            foreach (var book in query)
            {
                DisplayedBooks.Add(book);
            }

            TotalCount = DisplayedBooks.Count;
            HasBooks = TotalCount > 0;

            if (!HasBooks)
            {
                if (!string.IsNullOrWhiteSpace(ActiveKeyword))
                {
                    EmptyStateMessage = $"Không tìm thấy cuốn sách nào phù hợp với từ khóa \"{ActiveKeyword}\" và bộ lọc hiện tại.";
                }
                else if (SelectedStockFilter != DefaultStockFilter)
                {
                    EmptyStateMessage = $"Hiện không có cuốn sách nào ở trạng thái \"{SelectedStockFilter}\".";
                }
                else
                {
                    EmptyStateMessage = "Hiện chưa có sách nào trong danh mục.";
                }
            }
            else
            {
                EmptyStateMessage = string.Empty;
            }
        }

        [RelayCommand]
        private void ResetSearch()
        {
            SelectedStockFilter = DefaultStockFilter;
            SelectedSortOption = DefaultSortOption;
            ApplySearch(string.Empty);
        }

        [RelayCommand]
        private void ViewBookDetail(Book? book)
        {
            if (book == null)
            {
                return;
            }
            _navigationService.NavigateToBookDetail(book.MaSach);
        }

        [RelayCommand]
        private async Task AddToCart(Book? book)
        {
            if (book == null)
            {
                return;
            }

            var result = _cartService.AddToCart(book.MaSach, 1);
            if (!result.IsSuccess)
            {
                _dialogService.ShowError("Không thể thêm vào giỏ hàng", result.Message);
                return;
            }

            StatusBannerMessage = result.Message;
            IsStatusBannerVisible = true;
            await Task.Delay(2200);
            IsStatusBannerVisible = false;
        }
    }
}
