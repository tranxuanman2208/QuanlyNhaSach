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
        private readonly BookService _bookService;
        private readonly CartService _cartService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;

        private SACH? _selectedBook;
        public SACH? SelectedBook
        {
            get => _selectedBook;
            private set
            {
                // SACH (EF entity) không phát PropertyChanged; tồn kho được làm mới qua BookService.BooksChanged
                if (SetProperty(ref _selectedBook, value))
                {
                    NotifyBookDependentProperties();
                }
            }
        }

        /// <summary>Tồn kho hiện tại của sách đang xem (Soluongton là byte? trong DB).</summary>
        private int CurrentStock => SelectedBook?.Soluongton ?? 0;

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

                if (value > CurrentStock)
                {
                    _dialogService.ShowError(
                        "Vượt quá tồn kho",
                        $"Sách \"{SelectedBook.Tensach}\" hiện chỉ còn {CurrentStock} cuốn trong kho.");
                    SetProperty(ref _selectedQuantity, CurrentStock);
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

        public bool CanPurchase => IsBookFound && SelectedBook != null && CurrentStock > 0;
        public decimal EstimatedSubTotal => SelectedBook != null && CanPurchase ? (SelectedBook.Giaban ?? 0m) * SelectedQuantity : 0m;
        public string FormattedEstimatedSubTotal => $"{EstimatedSubTotal:N0} ₫";

        public BookDetailViewModel(
            BookService bookService,
            CartService cartService,
            INavigationService navigationService,
            IDialogService dialogService)
        {
            _bookService = bookService ?? throw new ArgumentNullException(nameof(bookService));
            _cartService = cartService ?? throw new ArgumentNullException(nameof(cartService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            _bookService.BooksChanged += OnBooksChanged;
        }

        public void OnNavigatedTo(object? parameter)
        {
            IsStatusVisible = false;
            StatusMessage = string.Empty;

            if (parameter is string maSach && !string.IsNullOrWhiteSpace(maSach))
            {
                LoadBook(maSach);
            }
            else if (parameter is SACH book)
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

        public void LoadBook(string maSach)
        {
            var book = _bookService.GetBookById(maSach);
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
            _selectedQuantity = (book.Soluongton ?? 0) > 0 ? 1 : 0;
            OnPropertyChanged(nameof(SelectedQuantity));
            NotifyBookDependentProperties();
        }

        /// <summary>
        /// Thay thế cơ chế PropertyChanged của model Book cũ: khi BookService báo dữ liệu sách thay đổi,
        /// tải lại sách đang xem từ DB và điều chỉnh số lượng chọn theo tồn kho mới.
        /// </summary>
        private void OnBooksChanged(object? sender, EventArgs e)
        {
            if (SelectedBook == null)
            {
                return;
            }

            var refreshed = _bookService.GetBookById(SelectedBook.MaSach);
            if (refreshed == null)
            {
                SelectedBook = null;
                IsBookFound = false;
                ErrorMessage = "Sách này không còn tồn tại trong hệ thống.";
                return;
            }

            SelectedBook = refreshed;

            if (CurrentStock <= 0)
            {
                _selectedQuantity = 0;
            }
            else if (_selectedQuantity > CurrentStock)
            {
                _selectedQuantity = CurrentStock;
            }
            else if (_selectedQuantity < 1)
            {
                _selectedQuantity = 1;
            }
            OnPropertyChanged(nameof(SelectedQuantity));
            NotifyBookDependentProperties();
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

        private bool CanIncreaseQuantity() => SelectedBook != null && SelectedBook.IsInStock && SelectedQuantity < CurrentStock;
        private bool CanDecreaseQuantity() => SelectedBook != null && SelectedBook.IsInStock && SelectedQuantity > 1;
        private bool CanAddToCart() => CanPurchase && SelectedQuantity >= 1 && SelectedBook != null && SelectedQuantity <= CurrentStock;

        [RelayCommand(CanExecute = nameof(CanIncreaseQuantity))]
        private void IncreaseQuantity()
        {
            if (SelectedBook != null && SelectedQuantity < CurrentStock)
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

            if (CurrentStock <= 0)
            {
                _dialogService.ShowError("Hết hàng", $"Sách \"{SelectedBook.Tensach}\" hiện đã hết hàng.");
                return;
            }

            if (SelectedQuantity < 1 || SelectedQuantity > CurrentStock)
            {
                _dialogService.ShowError(
                    "Số lượng không hợp lệ",
                    $"Vui lòng chọn số lượng từ 1 đến {CurrentStock}.");
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
