using System;
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

        public string BrandName => "BookNest";
        public string BrandTagline => "Nhà Sách Trực Tuyến";

        public string CustomerFullName => _authService.CurrentUser?.HoTen ?? "Khách hàng";
        public string CustomerUsername => _authService.CurrentUser?.Username ?? string.Empty;
        public string CustomerRoleLabel => _authService.CurrentUser?.RoleDisplayName ?? "Khách hàng";

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
            IDialogService dialogService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _cartService = cartService ?? throw new ArgumentNullException(nameof(cartService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            TotalQuantity = _cartService.TotalQuantity;
            _cartService.CartChanged += OnCartChanged;
            _navigationService.Navigated += OnNavigated;
            _authService.SessionChanged += OnSessionChanged;
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

        private void OnSessionChanged(object? sender, Models.User? user)
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
            _navigationService.NavigateToHome(string.Empty);
        }

        [RelayCommand]
        private void NavigateCart()
        {
            if (!EnsureAuthorizedSession())
            {
                return;
            }
            _navigationService.NavigateToCartCheckout();
        }

        [RelayCommand]
        private void NavigateProfile()
        {
            if (!EnsureAuthorizedSession())
            {
                return;
            }
            _navigationService.NavigateToCustomerProfile();
        }

        [RelayCommand]
        private void Logout()
        {
            _navigationService.LogoutToAuthWindow();
        }
    }
}
