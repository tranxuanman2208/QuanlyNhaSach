using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanlyNhaSach.Models;
using QuanlyNhaSach.Models.Enums;
using QuanlyNhaSach.Navigation;
using QuanlyNhaSach.Services;

namespace QuanlyNhaSach.ViewModels.Customer
{
    public partial class CartCheckoutViewModel : ObservableValidator, INavigationAware
    {
        private static readonly Regex PhoneTenDigitsRegex = new(@"^\d{10}$", RegexOptions.Compiled);

        private readonly MockDataService _mockDataService;
        private readonly CartService _cartService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
        private readonly AuthService? _authService;
        private readonly OrderService? _orderService;

        private bool _hasUserTouchedHoTen;
        private bool _hasUserTouchedSDT;
        private bool _hasUserTouchedDiaChi;
        private bool _hasUserTouchedPayment;

        public ObservableCollection<CartItem> CartItems => _cartService.Items;
        public ObservableCollection<Order> CompletedOrders => _mockDataService.Orders;

        public IReadOnlyList<string> PaymentMethods { get; } = new List<string>
        {
            "COD — Thanh toán khi nhận hàng",
            "Chuyển khoản"
        };

        public int TotalQuantity => _cartService.TotalQuantity;
        public decimal TotalAmount => _cartService.TotalAmount;
        public string FormattedTotalAmount => _cartService.FormattedTotalAmount;
        public bool IsCartEmpty => _cartService.IsEmpty;
        public bool HasCartItems => !_cartService.IsEmpty;
        public bool HasCompletedOrders => _mockDataService.Orders.Count > 0;

        private string _hoTen = string.Empty;
        [Required(ErrorMessage = "Họ và tên không được để trống.")]
        public string HoTen
        {
            get => _hoTen;
            set
            {
                if (SetProperty(ref _hoTen, value, true))
                {
                    _hasUserTouchedHoTen = true;
                    UpdateFieldErrors();
                    ConfirmCheckoutCommand.NotifyCanExecuteChanged();
                }
            }
        }

        private string _sdt = string.Empty;
        [Required(ErrorMessage = "Số điện thoại không được để trống.")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Số điện thoại phải gồm đúng 10 chữ số (0-9).")]
        public string SDT
        {
            get => _sdt;
            set
            {
                if (SetProperty(ref _sdt, value, true))
                {
                    _hasUserTouchedSDT = true;
                    UpdateFieldErrors();
                    ConfirmCheckoutCommand.NotifyCanExecuteChanged();
                }
            }
        }

        private string _diaChi = string.Empty;
        [Required(ErrorMessage = "Địa chỉ nhận hàng không được để trống.")]
        public string DiaChi
        {
            get => _diaChi;
            set
            {
                if (SetProperty(ref _diaChi, value, true))
                {
                    _hasUserTouchedDiaChi = true;
                    UpdateFieldErrors();
                    ConfirmCheckoutCommand.NotifyCanExecuteChanged();
                }
            }
        }

        private string _selectedPaymentMethod = "COD — Thanh toán khi nhận hàng";
        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
        public string SelectedPaymentMethod
        {
            get => _selectedPaymentMethod;
            set
            {
                if (SetProperty(ref _selectedPaymentMethod, value, true))
                {
                    _hasUserTouchedPayment = true;
                    UpdateFieldErrors();
                    OnPropertyChanged(nameof(NormalizedSelectedPayment));
                    ConfirmCheckoutCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public string NormalizedSelectedPayment =>
            string.IsNullOrWhiteSpace(SelectedPaymentMethod)
                ? "Chưa chọn"
                : MockDataService.NormalizePaymentMethod(SelectedPaymentMethod);

        private bool _isProcessingCheckout;
        public bool IsProcessingCheckout
        {
            get => _isProcessingCheckout;
            set
            {
                if (SetProperty(ref _isProcessingCheckout, value))
                {
                    ConfirmCheckoutCommand.NotifyCanExecuteChanged();
                }
            }
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasHoTenError))]
        private string _hoTenError = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasSDTError))]
        private string _sdtError = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasDiaChiError))]
        private string _diaChiError = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPaymentMethodError))]
        private string _paymentMethodError = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasGeneralError))]
        private string _generalErrorMessage = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasLastCreatedOrder))]
        private Order? _lastCreatedOrder;

        public bool HasHoTenError => !string.IsNullOrEmpty(HoTenError);
        public bool HasSDTError => !string.IsNullOrEmpty(SdtError);
        public bool HasDiaChiError => !string.IsNullOrEmpty(DiaChiError);
        public bool HasPaymentMethodError => !string.IsNullOrEmpty(PaymentMethodError);
        public bool HasGeneralError => !string.IsNullOrEmpty(GeneralErrorMessage);
        public bool HasLastCreatedOrder => LastCreatedOrder != null;

        public CartCheckoutViewModel(
            MockDataService mockDataService,
            CartService cartService,
            INavigationService navigationService,
            IDialogService dialogService,
            AuthService? authService = null,
            OrderService? orderService = null)
        {
            _mockDataService = mockDataService ?? throw new ArgumentNullException(nameof(mockDataService));
            _cartService = cartService ?? throw new ArgumentNullException(nameof(cartService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _authService = authService;
            _orderService = orderService;

            _cartService.CartChanged += OnCartChanged;
            _mockDataService.Orders.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasCompletedOrders));

            PreFillAuthenticatedCustomerDetails();
        }

        public void OnNavigatedTo(object? parameter)
        {
            GeneralErrorMessage = string.Empty;
            PreFillAuthenticatedCustomerDetails();
            NotifyCartProperties();
            ConfirmCheckoutCommand.NotifyCanExecuteChanged();
        }

        private void PreFillAuthenticatedCustomerDetails()
        {
            var currentUser = _authService?.CurrentUser;
            if (currentUser == null || currentUser.Role != UserRole.Customer)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_hoTen) && !string.IsNullOrWhiteSpace(currentUser.HoTen))
            {
                _hoTen = currentUser.HoTen;
                OnPropertyChanged(nameof(HoTen));
            }

            if (string.IsNullOrWhiteSpace(_sdt) && !string.IsNullOrWhiteSpace(currentUser.SDT))
            {
                _sdt = currentUser.SDT;
                OnPropertyChanged(nameof(SDT));
            }

            if (string.IsNullOrWhiteSpace(_diaChi) && !string.IsNullOrWhiteSpace(currentUser.DiaChi))
            {
                _diaChi = currentUser.DiaChi;
                OnPropertyChanged(nameof(DiaChi));
            }
        }

        private void OnCartChanged(object? sender, EventArgs e)
        {
            if (!_cartService.IsEmpty)
            {
                LastCreatedOrder = null;
            }
            NotifyCartProperties();
            ConfirmCheckoutCommand.NotifyCanExecuteChanged();
        }

        private void NotifyCartProperties()
        {
            OnPropertyChanged(nameof(TotalQuantity));
            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(FormattedTotalAmount));
            OnPropertyChanged(nameof(IsCartEmpty));
            OnPropertyChanged(nameof(HasCartItems));
        }

        private void UpdateFieldErrors(bool forceAll = false)
        {
            if (forceAll || _hasUserTouchedHoTen)
            {
                HoTenError = string.IsNullOrWhiteSpace(HoTen)
                    ? "Vui lòng nhập họ và tên người nhận."
                    : string.Empty;
            }

            if (forceAll || _hasUserTouchedSDT)
            {
                if (string.IsNullOrWhiteSpace(SDT))
                {
                    SdtError = "Vui lòng nhập số điện thoại liên hệ.";
                }
                else if (!PhoneTenDigitsRegex.IsMatch(SDT))
                {
                    SdtError = "Số điện thoại phải gồm đúng 10 chữ số (không chứa chữ cái hay khoảng trắng).";
                }
                else
                {
                    SdtError = string.Empty;
                }
            }

            if (forceAll || _hasUserTouchedDiaChi)
            {
                DiaChiError = string.IsNullOrWhiteSpace(DiaChi)
                    ? "Vui lòng nhập địa chỉ giao hàng."
                    : string.Empty;
            }

            if (forceAll || _hasUserTouchedPayment)
            {
                PaymentMethodError = !MockDataService.IsValidPaymentMethod(SelectedPaymentMethod)
                    ? "Vui lòng chọn phương thức thanh toán (COD hoặc Chuyển khoản)."
                    : string.Empty;
            }
        }

        public bool CanConfirmCheckout()
        {
            if (IsProcessingCheckout)
            {
                return false;
            }

            if (_cartService.IsEmpty || _cartService.Items.Count == 0)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(HoTen) || string.IsNullOrWhiteSpace(DiaChi))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(SDT) || !PhoneTenDigitsRegex.IsMatch(SDT))
            {
                return false;
            }

            if (!MockDataService.IsValidPaymentMethod(SelectedPaymentMethod))
            {
                return false;
            }

            foreach (var item in _cartService.Items)
            {
                if (item.SoLuongMua < 1 || item.Book == null || item.SoLuongMua > item.Book.SoLuongTon)
                {
                    return false;
                }
            }

            return true;
        }

        [RelayCommand]
        private void IncreaseQuantity(CartItem? item)
        {
            if (item == null)
            {
                return;
            }

            var result = _cartService.IncreaseQuantity(item);
            if (!result.IsSuccess)
            {
                _dialogService.ShowError("Vượt quá số lượng tồn kho", result.Message);
            }
            ConfirmCheckoutCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private void DecreaseQuantity(CartItem? item)
        {
            if (item == null)
            {
                return;
            }

            var result = _cartService.DecreaseQuantity(item);
            if (!result.IsSuccess)
            {
                _dialogService.ShowError("Không thể giảm số lượng", result.Message);
            }
            ConfirmCheckoutCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private void RemoveItem(CartItem? item)
        {
            if (item == null)
            {
                return;
            }

            _cartService.RemoveItem(item);
            ConfirmCheckoutCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private void ClearCart()
        {
            if (_cartService.IsEmpty)
            {
                return;
            }

            if (_dialogService.ShowConfirmation("Xác nhận xóa giỏ hàng", "Bạn có chắc chắn muốn xóa toàn bộ sách trong giỏ hàng không?"))
            {
                _cartService.ClearCart();
                ConfirmCheckoutCommand.NotifyCanExecuteChanged();
            }
        }

        [RelayCommand]
        private void ContinueShopping()
        {
            _navigationService.NavigateToHome();
        }

        [RelayCommand(CanExecute = nameof(CanConfirmCheckout))]
        private async Task ConfirmCheckoutAsync()
        {
            if (IsProcessingCheckout)
            {
                return;
            }

            ValidateAllProperties();
            UpdateFieldErrors(forceAll: true);
            GeneralErrorMessage = string.Empty;

            if (_cartService.IsEmpty || _cartService.Items.Count == 0)
            {
                GeneralErrorMessage = "Giỏ hàng của bạn đang trống. Vui lòng chọn sách trước khi đặt hàng.";
                _dialogService.ShowError("Giỏ hàng trống", GeneralErrorMessage);
                return;
            }

            if (HasHoTenError || HasSDTError || HasDiaChiError || HasPaymentMethodError)
            {
                GeneralErrorMessage = "Vui lòng kiểm tra và điền đầy đủ thông tin nhận hàng hợp lệ.";
                _dialogService.ShowError("Thông tin chưa hợp lệ", GeneralErrorMessage);
                return;
            }

            IsProcessingCheckout = true;
            try
            {
                await Task.Delay(200);

                int? authenticatedCustomerId = _authService?.CurrentUser?.MaND;
                OperationResult<Order> orderResult = _orderService != null
                    ? _orderService.CreateCustomerOrder(
                        HoTen,
                        SDT,
                        DiaChi,
                        SelectedPaymentMethod,
                        _cartService.Items.ToList(),
                        authenticatedCustomerId)
                    : _mockDataService.CreateOrder(
                        HoTen,
                        SDT,
                        DiaChi,
                        SelectedPaymentMethod,
                        _cartService.Items.ToList(),
                        authenticatedCustomerId);

                if (!orderResult.IsSuccess || orderResult.Data == null)
                {
                    GeneralErrorMessage = orderResult.Message;
                    _dialogService.ShowError("Đặt hàng không thành công", orderResult.Message);
                    return;
                }

                var createdOrder = orderResult.Data;
                LastCreatedOrder = createdOrder;

                _cartService.ClearCart();
                ResetFormFields();

                string successDetails =
                    $"Đơn hàng của bạn đã được tạo thành công!\n\n" +
                    $"• Mã đơn hàng: #{createdOrder.MaDH}\n" +
                    $"• Khách hàng: {createdOrder.HoTenKhachHang} ({createdOrder.SDTKhachHang})\n" +
                    $"• Phương thức thanh toán: {createdOrder.PhuongThucThanhToan}\n" +
                    $"• Tổng thanh toán: {createdOrder.FormattedTongTien}\n" +
                    $"• Trạng thái đơn hàng: {createdOrder.TrangThai}\n\n" +
                    $"[Đồng bộ dữ liệu In-Memory]: Đơn hàng này đã được thêm trực tiếp vào bộ nhớ dùng chung và xuất hiện tức thì tại Cổng Nhân viên (Staff Portal).";

                _dialogService.ShowInformation("Đặt hàng thành công", successDetails);
            }
            finally
            {
                IsProcessingCheckout = false;
                ConfirmCheckoutCommand.NotifyCanExecuteChanged();
            }
        }

        private void ResetFormFields()
        {
            _hasUserTouchedHoTen = false;
            _hasUserTouchedSDT = false;
            _hasUserTouchedDiaChi = false;
            _hasUserTouchedPayment = false;

            var currentUser = _authService?.CurrentUser;
            _hoTen = currentUser?.HoTen ?? string.Empty;
            _sdt = currentUser?.SDT ?? string.Empty;
            _diaChi = currentUser?.DiaChi ?? string.Empty;
            _selectedPaymentMethod = "COD — Thanh toán khi nhận hàng";

            ClearErrors();
            HoTenError = string.Empty;
            SdtError = string.Empty;
            DiaChiError = string.Empty;
            PaymentMethodError = string.Empty;
            GeneralErrorMessage = string.Empty;

            OnPropertyChanged(nameof(HoTen));
            OnPropertyChanged(nameof(SDT));
            OnPropertyChanged(nameof(DiaChi));
            OnPropertyChanged(nameof(SelectedPaymentMethod));
            OnPropertyChanged(nameof(NormalizedSelectedPayment));
        }
    }
}
