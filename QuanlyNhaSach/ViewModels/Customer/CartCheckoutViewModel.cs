using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
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

        // Đồng bộ với quy tắc của OrderService.CreateCustomerOrder (email phải kết thúc bằng @gmail.com).
        private const string RequiredEmailDomain = "@gmail.com";

        private readonly CartService _cartService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
        private readonly AuthService? _authService;
        private readonly OrderService? _orderService;
        private readonly BankTransferService _bankTransferService;

        private CancellationTokenSource? _qrPollingCts;

        private bool _hasUserTouchedHoTen;
        private bool _hasUserTouchedSDT;
        private bool _hasUserTouchedEmail;
        private bool _hasUserTouchedDiaChi;
        private bool _hasUserTouchedPayment;

        public ObservableCollection<CartItem> CartItems => _cartService.Items;

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

        public string BankName => _bankTransferService.Settings.BankName;
        public string BankId => _bankTransferService.Settings.BankId;
        public string BankAccountNo => _bankTransferService.Settings.AccountNo;
        public string BankAccountName => _bankTransferService.Settings.AccountName;
        public string PreviewTransferContent => _bankTransferService.BuildTransferContent(null, SDT);

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
                    OnPropertyChanged(nameof(PreviewTransferContent));
                    ConfirmCheckoutCommand.NotifyCanExecuteChanged();
                }
            }
        }

        private string _email = string.Empty;
        [Required(ErrorMessage = "Email không được để trống.")]
        public string Email
        {
            get => _email;
            set
            {
                if (SetProperty(ref _email, value, true))
                {
                    _hasUserTouchedEmail = true;
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
                    OnPropertyChanged(nameof(IsBankTransferSelected));
                    OnPropertyChanged(nameof(CheckoutButtonText));
                    ConfirmCheckoutCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public string NormalizedSelectedPayment =>
            string.IsNullOrWhiteSpace(SelectedPaymentMethod)
                ? "Chưa chọn"
                : NormalizePaymentMethod(SelectedPaymentMethod);

        public bool IsBankTransferSelected =>
            !string.IsNullOrWhiteSpace(SelectedPaymentMethod) &&
            SelectedPaymentMethod.Contains("Chuyển khoản", StringComparison.OrdinalIgnoreCase);

        public string CheckoutButtonText =>
            IsBankTransferSelected
                ? "💳 Hiển Thị Mã QR & Thanh Toán Đặt Hàng"
                : "✓ Xác Nhận Đặt Hàng";

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
        [NotifyPropertyChangedFor(nameof(HasEmailError))]
        private string _emailError = string.Empty;

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
        [NotifyPropertyChangedFor(nameof(LastOrderBannerTitle))]
        [NotifyPropertyChangedFor(nameof(LastOrderBannerSubtitle))]
        private DONHANG? _lastCreatedOrder;

        public string LastOrderBannerTitle =>
            LastCreatedOrder == null
                ? string.Empty
                : LastCreatedOrder.IsAwaitingBankTransferConfirmation
                    ? $"Đã báo chuyển khoản đến Nhân viên! Mã đơn hàng: {LastCreatedOrder.OrderCode}"
                    : $"Đặt hàng thành công! Mã đơn hàng: {LastCreatedOrder.OrderCode}";

        public string LastOrderBannerSubtitle =>
            LastCreatedOrder == null
                ? string.Empty
                : LastCreatedOrder.IsAwaitingBankTransferConfirmation
                    ? "Đơn hàng của bạn đang ở trạng thái Chờ nhân viên kiểm tra tiền về và xác nhận đặt hàng thành công."
                    : "Đơn hàng của bạn đã được lưu vào CSDL và đồng bộ với trang Lịch sử mua hàng cá nhân cũng như Cổng Nhân viên.";

        // ==================== QR PAYMENT MODAL PROPERTIES ====================

        [ObservableProperty]
        private bool _isQrPaymentModalOpen;

        [ObservableProperty]
        private bool _isLoadingQrImage;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasQrCodeImage))]
        [NotifyPropertyChangedFor(nameof(IsQrFallbackVisible))]
        private ImageSource? _qrCodeImageSource;

        [ObservableProperty]
        private DONHANG? _pendingBankTransferOrder;

        [ObservableProperty]
        private string _qrTransferContent = string.Empty;

        [ObservableProperty]
        private string _qrAmountFormatted = "0 ₫";

        [ObservableProperty]
        private string _qrAmountRaw = "0";

        [ObservableProperty]
        private string _qrCountdownText = "15:00";

        [ObservableProperty]
        private string _qrStatusMessage = "Đang chờ nhận tiền thanh toán chuyển khoản...";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasCopyFeedback))]
        private string _copyFeedbackText = string.Empty;

        public bool HasQrCodeImage => QrCodeImageSource != null;
        public bool IsQrFallbackVisible => !IsLoadingQrImage && QrCodeImageSource == null;
        public bool HasCopyFeedback => !string.IsNullOrWhiteSpace(CopyFeedbackText);

        public bool HasHoTenError => !string.IsNullOrEmpty(HoTenError);
        public bool HasSDTError => !string.IsNullOrEmpty(SdtError);
        public bool HasEmailError => !string.IsNullOrEmpty(EmailError);
        public bool HasDiaChiError => !string.IsNullOrEmpty(DiaChiError);
        public bool HasPaymentMethodError => !string.IsNullOrEmpty(PaymentMethodError);
        public bool HasGeneralError => !string.IsNullOrEmpty(GeneralErrorMessage);
        public bool HasLastCreatedOrder => LastCreatedOrder != null;

        public CartCheckoutViewModel(
            CartService cartService,
            INavigationService navigationService,
            IDialogService dialogService,
            AuthService? authService = null,
            OrderService? orderService = null,
            BankTransferService? bankTransferService = null)
        {
            _cartService = cartService ?? throw new ArgumentNullException(nameof(cartService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _authService = authService;
            _orderService = orderService;
            _bankTransferService = bankTransferService ?? new BankTransferService();

            _cartService.CartChanged += OnCartChanged;
            if (_orderService != null)
            {
                _orderService.OrderUpdated += OnOrderUpdatedFromService;
            }

            PreFillAuthenticatedCustomerDetails();
        }

        public void OnNavigatedTo(object? parameter)
        {
            GeneralErrorMessage = string.Empty;
            _bankTransferService.ReloadSettings();
            OnPropertyChanged(nameof(BankName));
            OnPropertyChanged(nameof(BankId));
            OnPropertyChanged(nameof(BankAccountNo));
            OnPropertyChanged(nameof(BankAccountName));
            OnPropertyChanged(nameof(PreviewTransferContent));

            PreFillAuthenticatedCustomerDetails();
            NotifyCartProperties();
            ConfirmCheckoutCommand.NotifyCanExecuteChanged();
        }

        private void OnOrderUpdatedFromService(object? sender, DONHANG? updatedOrder)
        {
            if (!IsQrPaymentModalOpen || PendingBankTransferOrder == null || updatedOrder == null)
            {
                return;
            }

            if (updatedOrder.MaDH == PendingBankTransferOrder.MaDH &&
                (updatedOrder.Dathanhtoan == true || updatedOrder.PaymentStatus == PaymentStatus.Paid))
            {
                Application.Current?.Dispatcher?.InvokeAsync(() =>
                {
                    if (IsQrPaymentModalOpen && PendingBankTransferOrder != null &&
                        PendingBankTransferOrder.MaDH == updatedOrder.MaDH)
                    {
                        CompleteBankTransferCheckout(updatedOrder);
                    }
                });
            }
        }

        private void PreFillAuthenticatedCustomerDetails()
        {
            var currentUser = _authService?.CurrentUser;
            if (currentUser == null || currentUser.Role != UserRole.Customer)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_hoTen) && !string.IsNullOrWhiteSpace(currentUser.Hoten))
            {
                _hoTen = currentUser.Hoten;
                OnPropertyChanged(nameof(HoTen));
            }

            if (string.IsNullOrWhiteSpace(_sdt) && !string.IsNullOrWhiteSpace(currentUser.SDT))
            {
                _sdt = currentUser.SDT;
                OnPropertyChanged(nameof(SDT));
                OnPropertyChanged(nameof(PreviewTransferContent));
            }

            if (string.IsNullOrWhiteSpace(_email) && !string.IsNullOrWhiteSpace(currentUser.Email))
            {
                _email = currentUser.Email;
                OnPropertyChanged(nameof(Email));
            }

            if (string.IsNullOrWhiteSpace(_diaChi) && !string.IsNullOrWhiteSpace(currentUser.Diachi))
            {
                _diaChi = currentUser.Diachi;
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

            if (forceAll || _hasUserTouchedEmail)
            {
                if (string.IsNullOrWhiteSpace(Email))
                {
                    EmailError = "Vui lòng nhập email người đặt hàng.";
                }
                else if (!IsValidCheckoutEmail(Email))
                {
                    EmailError = "Email phải có dạng ten@gmail.com (không chứa khoảng trắng).";
                }
                else
                {
                    EmailError = string.Empty;
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
                PaymentMethodError = !IsValidPaymentMethod(SelectedPaymentMethod)
                    ? "Vui lòng chọn phương thức thanh toán (COD hoặc Chuyển khoản)."
                    : string.Empty;
            }
        }

        private static bool IsValidCheckoutEmail(string? email)
        {
            return !string.IsNullOrWhiteSpace(email)
                && email.EndsWith(RequiredEmailDomain, StringComparison.Ordinal)
                && email.Length > RequiredEmailDomain.Length
                && !email.Any(char.IsWhiteSpace);
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

            if (!IsValidCheckoutEmail(Email))
            {
                return false;
            }

            if (!IsValidPaymentMethod(SelectedPaymentMethod))
            {
                return false;
            }

            foreach (var item in _cartService.Items)
            {
                if (item.SoLuongMua < 1 || item.Book == null || item.SoLuongMua > (item.Book.Soluongton ?? 0))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsValidPaymentMethod(string? method)
        {
            if (string.IsNullOrWhiteSpace(method))
            {
                return false;
            }
            return method.Contains("COD", StringComparison.OrdinalIgnoreCase) ||
                   method.Contains("Chuyển khoản", StringComparison.OrdinalIgnoreCase) ||
                   method.Contains("Tiền mặt", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizePaymentMethod(string? method)
        {
            if (string.IsNullOrWhiteSpace(method))
            {
                return "COD";
            }
            if (method.Contains("Chuyển khoản", StringComparison.OrdinalIgnoreCase))
            {
                return "Chuyển khoản";
            }
            return "COD";
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

            if (HasHoTenError || HasSDTError || HasEmailError || HasDiaChiError || HasPaymentMethodError)
            {
                GeneralErrorMessage = "Vui lòng kiểm tra và điền đầy đủ thông tin nhận hàng hợp lệ.";
                _dialogService.ShowError("Thông tin chưa hợp lệ", GeneralErrorMessage);
                return;
            }

            IsProcessingCheckout = true;
            try
            {
                await Task.Delay(150);

                string? authenticatedCustomerId = _authService?.CurrentUser?.MaND;
                OperationResult<DONHANG> orderResult = _orderService != null
                    ? _orderService.CreateCustomerOrder(
                        HoTen,
                        SDT,
                        Email,
                        DiaChi,
                        SelectedPaymentMethod,
                        _cartService.Items.ToList(),
                        authenticatedCustomerId)
                    : OperationResult<DONHANG>.Failure("Dịch vụ Đặt hàng chưa được khởi tạo.");

                if (!orderResult.IsSuccess || orderResult.Data == null)
                {
                    GeneralErrorMessage = orderResult.Message;
                    _dialogService.ShowError("Đặt hàng không thành công", orderResult.Message);
                    return;
                }

                var createdOrder = orderResult.Data;

                // Nếu khách hàng chọn Chuyển khoản -> Mở hộp thoại QR cá nhân và đợi nhận tiền thanh toán thành công
                if (IsBankTransferSelected)
                {
                    await OpenQrPaymentModalAsync(createdOrder);
                    return;
                }

                // Nếu chọn COD -> Hoàn tất đặt hàng ngay
                FinalizeSuccessfulOrder(
                    createdOrder,
                    $"Đơn hàng của bạn đã được tạo thành công!\n\n" +
                    $"• Mã đơn hàng: #{createdOrder.MaDH}\n" +
                    $"• Khách hàng: {createdOrder.HoTenKhachHang} ({createdOrder.SDTKhachHang})\n" +
                    $"• Phương thức thanh toán: {createdOrder.PhuongThucThanhToan}\n" +
                    $"• Tổng thanh toán: {createdOrder.FormattedTongTien}\n" +
                    $"• Trạng thái đơn hàng: {createdOrder.TrangThai}");
            }
            finally
            {
                IsProcessingCheckout = false;
                ConfirmCheckoutCommand.NotifyCanExecuteChanged();
            }
        }

        private async Task OpenQrPaymentModalAsync(DONHANG pendingOrder)
        {
            StopQrPolling();

            _bankTransferService.ReloadSettings();
            OnPropertyChanged(nameof(BankName));
            OnPropertyChanged(nameof(BankId));
            OnPropertyChanged(nameof(BankAccountNo));
            OnPropertyChanged(nameof(BankAccountName));

            PendingBankTransferOrder = pendingOrder;
            QrTransferContent = _bankTransferService.BuildTransferContent(pendingOrder.MaDH, pendingOrder.SDTKhachHang);
            QrAmountFormatted = pendingOrder.FormattedTongTien;
            QrAmountRaw = Math.Round(pendingOrder.TongTien, 0).ToString("0");
            QrCountdownText = "15:00";
            CopyFeedbackText = string.Empty;
            QrStatusMessage = $"Đang chờ Nhân viên kiểm tra tài khoản và duyệt thanh toán cho đơn {pendingOrder.OrderCode}...";

            IsLoadingQrImage = true;
            QrCodeImageSource = null;
            IsQrPaymentModalOpen = true;

            _qrPollingCts = new CancellationTokenSource();
            var token = _qrPollingCts.Token;

            try
            {
                var qrImage = await _bankTransferService.LoadQrImageAsync(pendingOrder.TongTien, QrTransferContent, token);
                if (!token.IsCancellationRequested)
                {
                    QrCodeImageSource = qrImage;
                }
            }
            finally
            {
                if (!token.IsCancellationRequested)
                {
                    IsLoadingQrImage = false;
                    OnPropertyChanged(nameof(IsQrFallbackVisible));
                }
            }

            if (!token.IsCancellationRequested)
            {
                _ = PollBankTransferPaymentStatusAsync(pendingOrder.MaDH, token);
            }
        }

        private async Task PollBankTransferPaymentStatusAsync(int maDH, CancellationToken token)
        {
            DateTime expiresAt = DateTime.Now.AddMinutes(15);

            try
            {
                while (!token.IsCancellationRequested && IsQrPaymentModalOpen)
                {
                    TimeSpan remaining = expiresAt - DateTime.Now;
                    if (remaining <= TimeSpan.Zero)
                    {
                        QrCountdownText = "00:00";
                        QrStatusMessage = "Đã hết thời gian chờ hiển thị QR. Nếu bạn đã chuyển khoản, vui lòng bấm 'Tôi Đã Chuyển Khoản' để báo nhân viên duyệt.";
                        break;
                    }

                    QrCountdownText = $"{(int)remaining.TotalMinutes:D2}:{remaining.Seconds:D2}";

                    if (_orderService != null)
                    {
                        var freshOrder = await Task.Run(() => _orderService.GetFreshOrderFromDatabase(maDH), token);
                        if (freshOrder != null && (freshOrder.Dathanhtoan == true || freshOrder.PaymentStatus == PaymentStatus.Paid))
                        {
                            CompleteBankTransferCheckout(freshOrder);
                            return;
                        }
                    }

                    await Task.Delay(1000, token);
                }
            }
            catch (OperationCanceledException)
            {
                // Bỏ qua khi đóng hộp thoại QR
            }
        }

        [RelayCommand]
        private void SubmitWaitingStaffVerification()
        {
            if (PendingBankTransferOrder == null)
            {
                return;
            }

            // Kiểm tra lần cuối xem nhân viên đã duyệt tiền về chưa
            if (_orderService != null)
            {
                var fresh = _orderService.GetFreshOrderFromDatabase(PendingBankTransferOrder.MaDH);
                if (fresh != null && (fresh.Dathanhtoan == true || fresh.PaymentStatus == PaymentStatus.Paid))
                {
                    CompleteBankTransferCheckout(fresh);
                    return;
                }
            }

            var waitingOrder = PendingBankTransferOrder;
            StopQrPolling();
            IsQrPaymentModalOpen = false;
            PendingBankTransferOrder = null;

            FinalizeSuccessfulOrder(
                waitingOrder,
                $"Đã gửi thông báo đã chuyển khoản cho đơn hàng {waitingOrder.OrderCode} đến Nhân viên!\n\n" +
                $"• Khách hàng: {waitingOrder.HoTenKhachHang} ({waitingOrder.SDTKhachHang})\n" +
                $"• Số tiền chuyển khoản: {waitingOrder.FormattedTongTien}\n" +
                $"• Nội dung CK: {QrTransferContent}\n" +
                $"• Trạng thái thanh toán: Chờ nhân viên duyệt CK\n" +
                $"• Trạng thái đơn hàng: {waitingOrder.TrangThai}\n\n");
        }

        [RelayCommand]
        private void CancelQrPayment()
        {
            if (PendingBankTransferOrder == null)
            {
                StopQrPolling();
                IsQrPaymentModalOpen = false;
                return;
            }

            bool confirmCancel = _dialogService.ShowConfirmation(
                "Hủy phiên thanh toán QR",
                "Bạn chưa hoàn tất chuyển khoản và muốn hủy phiên thanh toán này để quay lại chỉnh sửa giỏ hàng?");

            if (!confirmCancel)
            {
                return;
            }

            int maDH = PendingBankTransferOrder.MaDH;
            StopQrPolling();
            IsQrPaymentModalOpen = false;
            PendingBankTransferOrder = null;

            _orderService?.CancelPendingBankTransferOrder(maDH);
        }

        [RelayCommand]
        private void CopyBankAccountNo()
        {
            CopyToClipboard(BankAccountNo, $"Đã sao chép Số tài khoản: {BankAccountNo}");
        }

        [RelayCommand]
        private void CopyTransferAmount()
        {
            CopyToClipboard(QrAmountRaw, $"Đã sao chép Số tiền: {QrAmountFormatted}");
        }

        [RelayCommand]
        private void CopyTransferContent()
        {
            CopyToClipboard(QrTransferContent, $"Đã sao chép Nội dung CK: {QrTransferContent}");
        }

        private async void CopyToClipboard(string text, string feedback)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            try
            {
                Clipboard.SetText(text);
                CopyFeedbackText = feedback;
                await Task.Delay(2500);
                if (CopyFeedbackText == feedback)
                {
                    CopyFeedbackText = string.Empty;
                }
            }
            catch
            {
                // Ignore clipboard lock errors
            }
        }

        private void CompleteBankTransferCheckout(DONHANG confirmedOrder)
        {
            StopQrPolling();
            IsQrPaymentModalOpen = false;
            PendingBankTransferOrder = null;

            FinalizeSuccessfulOrder(
                confirmedOrder,
                $"Đã nhận tiền chuyển khoản và xác nhận đặt hàng thành công!\n\n" +
                $"• Mã đơn hàng: {confirmedOrder.OrderCode}\n" +
                $"• Khách hàng: {confirmedOrder.HoTenKhachHang} ({confirmedOrder.SDTKhachHang})\n" +
                $"• Phương thức thanh toán: Chuyển khoản QR (Đã thanh toán)\n" +
                $"• Số tiền đã nhận: {confirmedOrder.FormattedTongTien}\n" +
                $"• Trạng thái đơn hàng: {confirmedOrder.TrangThai}");
        }

        private void FinalizeSuccessfulOrder(DONHANG order, string dialogMessage, string dialogTitle = "Đặt hàng thành công")
        {
            LastCreatedOrder = order;

            _cartService.ClearCart();
            ResetFormFields();
            _dialogService.ShowInformation(dialogTitle, dialogMessage);
        }

        private void StopQrPolling()
        {
            try
            {
                _qrPollingCts?.Cancel();
                _qrPollingCts?.Dispose();
            }
            catch
            {
                // Ignore cancellation disposal errors
            }
            finally
            {
                _qrPollingCts = null;
            }
        }

        private void ResetFormFields()
        {
            _hasUserTouchedHoTen = false;
            _hasUserTouchedSDT = false;
            _hasUserTouchedEmail = false;
            _hasUserTouchedDiaChi = false;
            _hasUserTouchedPayment = false;

            var currentUser = _authService?.CurrentUser;
            _hoTen = currentUser?.Hoten ?? string.Empty;
            _sdt = currentUser?.SDT ?? string.Empty;
            _email = currentUser?.Email ?? string.Empty;
            _diaChi = currentUser?.Diachi ?? string.Empty;
            _selectedPaymentMethod = "COD — Thanh toán khi nhận hàng";

            ClearErrors();
            HoTenError = string.Empty;
            SdtError = string.Empty;
            EmailError = string.Empty;
            DiaChiError = string.Empty;
            PaymentMethodError = string.Empty;
            GeneralErrorMessage = string.Empty;

            OnPropertyChanged(nameof(HoTen));
            OnPropertyChanged(nameof(SDT));
            OnPropertyChanged(nameof(Email));
            OnPropertyChanged(nameof(DiaChi));
            OnPropertyChanged(nameof(SelectedPaymentMethod));
            OnPropertyChanged(nameof(NormalizedSelectedPayment));
            OnPropertyChanged(nameof(IsBankTransferSelected));
            OnPropertyChanged(nameof(CheckoutButtonText));
            OnPropertyChanged(nameof(PreviewTransferContent));
        }
    }
}
