using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanlyNhaSach.Models.Enums;
using QuanlyNhaSach.Services;

namespace QuanlyNhaSach.ViewModels
{
    public partial class CustomerMainViewModel : ObservableObject
    {
        private readonly AuthService _authService;
        private readonly CartService _cartService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
        private readonly BookService _bookService;

        public string BrandName => "BookNest";
        public string BrandTagline => "Nhà Sách Trực Tuyến";

        public string CustomerFullName => _authService.CurrentUser?.Hoten ?? "Khách hàng";
        public string CustomerUsername => _authService.CurrentUser?.Tendangnhap ?? string.Empty;
        public string CustomerRoleLabel => _authService.CurrentUser?.RoleDisplayName ?? "Khách hàng";

        public ObservableCollection<string> BookTitleSuggestions { get; } = new();

        [ObservableProperty]
        private bool _isBookSuggestionsOpen;

        [ObservableProperty]
        private string _searchKeyword = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasCartItems))]
        private int _totalQuantity;

        [ObservableProperty]
        private string _currentPageKey = "Home";

        public bool HasCartItems => TotalQuantity > 0;
        public bool IsHomeActive => CurrentPageKey == "Home";
        public bool IsCartActive => CurrentPageKey == "CartCheckout";
        public bool IsProfileActive => CurrentPageKey == "CustomerProfile";

        public CustomerMainViewModel(
            AuthService authService,
            CartService cartService,
            INavigationService navigationService,
            IDialogService dialogService,
            BookService? bookService = null)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _cartService = cartService ?? throw new ArgumentNullException(nameof(cartService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _bookService = bookService ?? App.BookService ?? new BookService();

            TotalQuantity = _cartService.TotalQuantity;
            _cartService.CartChanged += OnCartChanged;
            _navigationService.Navigated += OnNavigated;
            _authService.SessionChanged += OnSessionChanged;
        }

        partial void OnSearchKeywordChanged(string value)
        {
            UpdateBookSuggestions(value);
        }

        private void UpdateBookSuggestions(string keyword)
        {
            BookTitleSuggestions.Clear();
            if (string.IsNullOrWhiteSpace(keyword) || keyword.Trim().Length < 1)
            {
                IsBookSuggestionsOpen = false;
                return;
            }

            string term = keyword.Trim().ToLowerInvariant();
            var allBooks = _bookService.GetAllBooks();
            var matches = allBooks
                .Where(b => !string.IsNullOrWhiteSpace(b.Tensach) && b.Tensach.ToLowerInvariant().Contains(term))
                .Select(b => b.Tensach!.Trim())
                .Distinct()
                .Take(6)
                .ToList();

            foreach (var title in matches)
            {
                BookTitleSuggestions.Add(title);
            }

            IsBookSuggestionsOpen = BookTitleSuggestions.Count > 0;
        }

        [RelayCommand]
        public void SelectSuggestion(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return;
            SearchKeyword = title;
            IsBookSuggestionsOpen = false;
            Search();
        }

        [RelayCommand]
        public void CloseSuggestions()
        {
            IsBookSuggestionsOpen = false;
        }

        public bool EnsureAuthorizedSession()
        {
            if (!_authService.HasRole(UserRole.Customer))
            {
                _dialogService.ShowError(
                    "Truy cập không hợp lệ",
                    "Phiên làm việc Khách hàng không hợp lệ hoặc đã hết hạn. Vui lòng đăng nhập lại.");
                _navigationService.LogoutToAuthWindow();
                return false;
            }
            return true;
        }

        private void OnSessionChanged(object? sender, Models.NGUOIDUNG? user)
        {
            OnPropertyChanged(nameof(CustomerFullName));
            OnPropertyChanged(nameof(CustomerUsername));
            OnPropertyChanged(nameof(CustomerRoleLabel));
        }

        private void OnCartChanged(object? sender, EventArgs e)
        {
            TotalQuantity = _cartService.TotalQuantity;
        }

        private void OnNavigated(string pageKey)
        {
            CurrentPageKey = pageKey;
            OnPropertyChanged(nameof(IsHomeActive));
            OnPropertyChanged(nameof(IsCartActive));
            OnPropertyChanged(nameof(IsProfileActive));
        }

        [RelayCommand]
        private void Search()
        {
            if (!EnsureAuthorizedSession())
            {
                return;
            }
            IsBookSuggestionsOpen = false;
            _navigationService.NavigateToHome(SearchKeyword);
        }

        [RelayCommand]
        private void ClearSearch()
        {
            if (!EnsureAuthorizedSession())
            {
                return;
            }
            SearchKeyword = string.Empty;
            IsBookSuggestionsOpen = false;
            _navigationService.NavigateToHome(string.Empty);
        }

        [RelayCommand]
        private void NavigateHome()
        {
            if (!EnsureAuthorizedSession())
            {
                return;
            }
            SearchKeyword = string.Empty;
            IsBookSuggestionsOpen = false;
            _navigationService.NavigateToHome(string.Empty);
        }

        [RelayCommand]
        private void NavigateCart()
        {
            if (!EnsureAuthorizedSession())
            {
                return;
            }
            IsBookSuggestionsOpen = false;
            _navigationService.NavigateToCartCheckout();
        }

        [RelayCommand]
        private void NavigateProfile()
        {
            if (!EnsureAuthorizedSession())
            {
                return;
            }
            IsBookSuggestionsOpen = false;
            _navigationService.NavigateToCustomerProfile();
        }

        [RelayCommand]
        private void NavigateOrderHistory()
        {
            if (!EnsureAuthorizedSession())
            {
                return;
            }
            IsBookSuggestionsOpen = false;
            _navigationService.NavigateToCustomerProfile("PurchaseHistory");
        }

        [RelayCommand]
        private void Logout()
        {
            IsBookSuggestionsOpen = false;
            if (!_dialogService.ShowConfirmation(
                    "Xác nhận đăng xuất",
                    "Bạn có chắc chắn muốn đăng xuất không?"))
            {
                return;
            }

            _navigationService.LogoutToAuthWindow();
        }
    }
}
