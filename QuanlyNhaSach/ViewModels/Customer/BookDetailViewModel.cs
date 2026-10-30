using System;
using System.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanlyNhaSach.Models;
using QuanlyNhaSach.Navigation;
using QuanlyNhaSach.Services;

namespace QuanlyNhaSach.ViewModels.Customer
{
    public partial class BookDetailViewModel : ObservableObject, INavigationAware
    {
        private readonly MockDataService _mockDataService;
        private readonly CartService _cartService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;

        private Book? _selectedBook;
        public Book? SelectedBook
        {
            get => _selectedBook;
            private set
            {
                if (_selectedBook != null)
                {
                    PropertyChangedEventManager.RemoveHandler(_selectedBook, OnSelectedBookPropertyChanged, string.Empty);
                }

                if (SetProperty(ref _selectedBook, value))
                {
                    if (_selectedBook != null)
                    {
                        PropertyChangedEventManager.AddHandler(_selectedBook, OnSelectedBookPropertyChanged, string.Empty);
                    }
                    NotifyBookDependentProperties();
                }
            }
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsBookNotFound))]
        private bool _isBookFound;

        public bool IsBookNotFound => !IsBookFound;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        private int _selectedQuantity = 1;
        public int SelectedQuantity
        {
            get => _selectedQuantity;
            set
            {
                if (SelectedBook == null || !SelectedBook.IsInStock)
                {
                    SetProperty(ref _selectedQuantity, 0);
                    NotifyQuantityCommands();
                    return;
                }

                if (value < 1)
                {
                    _dialogService.ShowError("Số lượng không hợp lệ", "Số lượng mua tối thiểu phải là 1 cuốn.");
                    OnPropertyChanged(nameof(SelectedQuantity));
                    return;
                }

                if (value > SelectedBook.SoLuongTon)
                {
                    _dialogService.ShowError(
                        "Vượt quá tồn kho",
                        $"Sách \"{SelectedBook.TenSach}\" hiện chỉ còn {SelectedBook.SoLuongTon} cuốn trong kho.");
                    SetProperty(ref _selectedQuantity, SelectedBook.SoLuongTon);
                    NotifyQuantityCommands();
                    return;
                }

                if (SetProperty(ref _selectedQuantity, value))
                {
                    NotifyQuantityCommands();
                }
            }
        }

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isStatusVisible;

        public bool CanPurchase => IsBookFound && SelectedBook != null && SelectedBook.SoLuongTon > 0;
        public decimal EstimatedSubTotal => SelectedBook != null && CanPurchase ? SelectedBook.GiaBan * SelectedQuantity : 0m;
        public string FormattedEstimatedSubTotal => $"{EstimatedSubTotal:N0} ₫";

        public BookDetailViewModel(
            MockDataService mockDataService,
            CartService cartService,
            INavigationService navigationService,
            IDialogService dialogService)
        {
            _mockDataService = mockDataService ?? throw new ArgumentNullException(nameof(mockDataService));
            _cartService = cartService ?? throw new ArgumentNullException(nameof(cartService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        }

        public void OnNavigatedTo(object? parameter)
        {
            IsStatusVisible = false;
            StatusMessage = string.Empty;

            if (parameter is int maSach)
            {
                LoadBook(maSach);
            }
            else if (parameter is Book book)
            {
                LoadBook(book.MaSach);
            }
            else
            {
                SelectedBook = null;
                IsBookFound = false;
                ErrorMessage = "Không xác định được mã sách cần xem chi tiết.";
            }
        }

        public void LoadBook(int maSach)
        {
            var book = _mockDataService.GetBookById(maSach);
            if (book == null)
            {
                SelectedBook = null;
                IsBookFound = false;
                ErrorMessage = $"Không tìm thấy sách có mã #{maSach} trong hệ thống.";
                return;
            }

            SelectedBook = book;
            IsBookFound = true;
            ErrorMessage = string.Empty;
            _selectedQuantity = book.SoLuongTon > 0 ? 1 : 0;
            OnPropertyChanged(nameof(SelectedQuantity));
            NotifyBookDependentProperties();
        }

        private void OnSelectedBookPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Book.SoLuongTon) || string.IsNullOrEmpty(e.PropertyName))
            {
                if (SelectedBook != null)
                {
                    if (SelectedBook.SoLuongTon <= 0)
                    {
                        _selectedQuantity = 0;
                    }
                    else if (_selectedQuantity > SelectedBook.SoLuongTon)
                    {
                        _selectedQuantity = SelectedBook.SoLuongTon;
                    }
                    else if (_selectedQuantity < 1)
                    {
                        _selectedQuantity = 1;
                    }
                    OnPropertyChanged(nameof(SelectedQuantity));
                }
                NotifyBookDependentProperties();
            }
        }

        private void NotifyBookDependentProperties()
        {
            OnPropertyChanged(nameof(CanPurchase));
            OnPropertyChanged(nameof(EstimatedSubTotal));
            OnPropertyChanged(nameof(FormattedEstimatedSubTotal));
            NotifyQuantityCommands();
        }

        private void NotifyQuantityCommands()
        {
            OnPropertyChanged(nameof(EstimatedSubTotal));
            OnPropertyChanged(nameof(FormattedEstimatedSubTotal));
            IncreaseQuantityCommand.NotifyCanExecuteChanged();
            DecreaseQuantityCommand.NotifyCanExecuteChanged();
            AddToCartCommand.NotifyCanExecuteChanged();
        }

        private bool CanIncreaseQuantity() => SelectedBook != null && SelectedBook.IsInStock && SelectedQuantity < SelectedBook.SoLuongTon;
        private bool CanDecreaseQuantity() => SelectedBook != null && SelectedBook.IsInStock && SelectedQuantity > 1;
        private bool CanAddToCart() => CanPurchase && SelectedQuantity >= 1 && SelectedBook != null && SelectedQuantity <= SelectedBook.SoLuongTon;

        [RelayCommand(CanExecute = nameof(CanIncreaseQuantity))]
        private void IncreaseQuantity()
        {
            if (SelectedBook != null && SelectedQuantity < SelectedBook.SoLuongTon)
            {
                SelectedQuantity++;
            }
        }

        [RelayCommand(CanExecute = nameof(CanDecreaseQuantity))]
        private void DecreaseQuantity()
        {
            if (SelectedQuantity > 1)
            {
                SelectedQuantity--;
            }
        }

        [RelayCommand(CanExecute = nameof(CanAddToCart))]
        private async Task AddToCart()
        {
            if (SelectedBook == null)
            {
                return;
            }

            if (SelectedBook.SoLuongTon <= 0)
            {
                _dialogService.ShowError("Hết hàng", $"Sách \"{SelectedBook.TenSach}\" hiện đã hết hàng.");
                return;
            }

            if (SelectedQuantity < 1 || SelectedQuantity > SelectedBook.SoLuongTon)
            {
                _dialogService.ShowError(
                    "Số lượng không hợp lệ",
                    $"Vui lòng chọn số lượng từ 1 đến {SelectedBook.SoLuongTon}.");
                return;
            }

            var result = _cartService.AddToCart(SelectedBook.MaSach, SelectedQuantity);
            if (!result.IsSuccess)
            {
                _dialogService.ShowError("Không thể thêm vào giỏ hàng", result.Message);
                return;
            }

            StatusMessage = result.Message;
            IsStatusVisible = true;
            await Task.Delay(2200);
            IsStatusVisible = false;
        }

        [RelayCommand]
        private void BackToHome()
        {
            _navigationService.NavigateToHome();
        }

        [RelayCommand]
        private void GoToCart()
        {
            _navigationService.NavigateToCartCheckout();
        }
    }
}
