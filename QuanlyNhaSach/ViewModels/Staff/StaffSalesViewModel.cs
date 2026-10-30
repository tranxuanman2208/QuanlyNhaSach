using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanlyNhaSach.Models;
using QuanlyNhaSach.Models.Enums;
using QuanlyNhaSach.Navigation;
using QuanlyNhaSach.Services;

namespace QuanlyNhaSach.ViewModels.Staff
{
    public partial class StaffSalesViewModel : ObservableObject, INavigationAware
    {
        private readonly MockDataService _mockDataService;
        private readonly OrderService _orderService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;

        public ObservableCollection<Order> AvailableOrders { get; } = new();
        public ObservableCollection<OrderItem> SelectedOrderItems { get; } = new();

        public IReadOnlyList<string> PaymentMethodOptions { get; } = new List<string>
        {
            PaymentMethod.Cash.ToVietnameseLabel(),
            PaymentMethod.BankTransfer.ToVietnameseLabel(),
            PaymentMethod.COD.ToVietnameseLabel()
        };

        [ObservableProperty]
        private string _orderSearchKeyword = string.Empty;

        private Order? _selectedOrder;
        public Order? SelectedOrder
        {
            get => _selectedOrder;
            set
            {
                if (SetProperty(ref _selectedOrder, value))
                {
                    LoadSelectedOrderDetails();
                }
            }
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsCashSelected))]
        [NotifyPropertyChangedFor(nameof(IsBankTransferSelected))]
        [NotifyPropertyChangedFor(nameof(IsCodSelected))]
        [NotifyPropertyChangedFor(nameof(IsAmountInputRequired))]
        [NotifyPropertyChangedFor(nameof(CanShowCodDeliveryCheck))]
        [NotifyPropertyChangedFor(nameof(PaymentMethodGuideText))]
        [NotifyPropertyChangedFor(nameof(FormattedChangeAmount))]
        private string _selectedPaymentMethodLabel = PaymentMethod.Cash.ToVietnameseLabel();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FormattedChangeAmount))]
        private string _paymentAmountText = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsAmountInputRequired))]
        [NotifyPropertyChangedFor(nameof(PaymentMethodGuideText))]
        private bool _confirmCodCollectionOnDelivery;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasFeedback))]
        private string _feedbackMessage = string.Empty;

        [ObservableProperty]
        private bool _isFeedbackSuccess = true;

        public bool HasSelectedOrder => SelectedOrder != null;
        public bool HasFeedback => !string.IsNullOrWhiteSpace(FeedbackMessage);

        public bool IsCashSelected => SelectedPaymentMethodLabel == PaymentMethod.Cash.ToVietnameseLabel();
        public bool IsBankTransferSelected => SelectedPaymentMethodLabel == PaymentMethod.BankTransfer.ToVietnameseLabel();
        public bool IsCodSelected => SelectedPaymentMethodLabel == PaymentMethod.COD.ToVietnameseLabel();

        public bool CanShowCodDeliveryCheck =>
            IsCodSelected &&
            SelectedOrder != null &&
            SelectedOrder.Status == OrderStatus.Shipping &&
            SelectedOrder.StockOutStatus == StockOutStatus.Issued &&
            SelectedOrder.PaymentStatus != PaymentStatus.Paid;

        public bool IsAmountInputRequired =>
            IsCashSelected || IsBankTransferSelected || (IsCodSelected && ConfirmCodCollectionOnDelivery);

        public string FormattedChangeAmount
        {
            get
            {
                if (SelectedOrder == null || !IsAmountInputRequired)
                {
                    return "0 ₫";
                }

                if (TryParsePaymentAmount(out decimal amount) && amount > SelectedOrder.TongTien)
                {
                    return $"{amount - SelectedOrder.TongTien:N0} ₫";
                }

                return "0 ₫";
            }
        }

        public string PaymentMethodGuideText
        {
            get
            {
                if (IsCashSelected)
                {
                    return "Tiền mặt: Nhập số tiền khách đưa (>= tổng tiền đơn hàng). Hệ thống tự động tính tiền thừa và ghi nhận trạng thái 'Đã thanh toán'.";
                }

                if (IsBankTransferSelected)
                {
                    return "Chuyển khoản (Mô phỏng): Xác nhận nhân viên đã kiểm tra khoản chuyển khoản nội bộ trên dữ liệu mẫu (không kết nối ngân hàng thực tế).";
                }

                if (IsCodSelected)
                {
                    return ConfirmCodCollectionOnDelivery
                        ? "Thu hộ COD khi giao hàng: Xác nhận nhân viên giao hàng đã thu đủ tiền mặt từ khách và hoàn tất đơn hàng."
                        : "COD (Thanh toán khi nhận hàng): Ghi nhận trạng thái 'Thu hộ (COD)'. Đơn hàng chưa được tính là Đã thanh toán hay Hoàn tất cho đến khi giao sách và thu tiền thực tế.";
                }

                return string.Empty;
            }
        }

        public StaffSalesViewModel(
            MockDataService mockDataService,
            OrderService orderService,
            INavigationService navigationService,
            IDialogService dialogService)
        {
            _mockDataService = mockDataService ?? throw new ArgumentNullException(nameof(mockDataService));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            _mockDataService.DataChanged += (_, _) => RefreshAvailableOrders(preserveSelection: true);
            RefreshAvailableOrders();
        }

        public void OnNavigatedTo(object? parameter)
        {
            FeedbackMessage = string.Empty;
            RefreshAvailableOrders(preserveSelection: true);

            if (parameter is int orderId)
            {
                var found = AvailableOrders.FirstOrDefault(o => o.MaDH == orderId);
                if (found != null)
                {
                    SelectedOrder = found;
                }
            }
        }

        public void RefreshAvailableOrders(bool preserveSelection = true)
        {
            int? currentId = preserveSelection ? SelectedOrder?.MaDH : null;

            var orders = _orderService.SearchOrders(OrderSearchKeyword);
            AvailableOrders.Clear();
            foreach (var order in orders)
            {
                AvailableOrders.Add(order);
            }

            if (currentId.HasValue)
            {
                SelectedOrder = AvailableOrders.FirstOrDefault(o => o.MaDH == currentId.Value)
                             ?? AvailableOrders.FirstOrDefault();
            }
            else if (SelectedOrder == null && AvailableOrders.Count > 0)
            {
                SelectedOrder = AvailableOrders.FirstOrDefault(o => o.CanRecordPayment)
                             ?? AvailableOrders[0];
            }
        }

        private void LoadSelectedOrderDetails()
        {
            SelectedOrderItems.Clear();
            ConfirmCodCollectionOnDelivery = false;

            if (SelectedOrder != null)
            {
                foreach (var item in SelectedOrder.Items)
                {
                    SelectedOrderItems.Add(item);
                }

                SelectedPaymentMethodLabel = SelectedOrder.Method.ToVietnameseLabel();
                PaymentAmountText = SelectedOrder.TongTien.ToString("0", CultureInfo.InvariantCulture);
            }
            else
            {
                PaymentAmountText = string.Empty;
            }

            OnPropertyChanged(nameof(HasSelectedOrder));
            OnPropertyChanged(nameof(CanShowCodDeliveryCheck));
            OnPropertyChanged(nameof(IsAmountInputRequired));
            OnPropertyChanged(nameof(FormattedChangeAmount));
            OnPropertyChanged(nameof(PaymentMethodGuideText));
        }

        [RelayCommand]
        private void SearchOrder()
        {
            FeedbackMessage = string.Empty;
            RefreshAvailableOrders(preserveSelection: false);
            if (AvailableOrders.Count == 0)
            {
                IsFeedbackSuccess = false;
                FeedbackMessage = $"Không tìm thấy đơn hàng nào khớp với từ khóa \"{OrderSearchKeyword}\".";
            }
        }

        [RelayCommand]
        private void ClearSearch()
        {
            OrderSearchKeyword = string.Empty;
            FeedbackMessage = string.Empty;
            RefreshAvailableOrders(preserveSelection: false);
        }

        [RelayCommand]
        private void FillExactAmount()
        {
            if (SelectedOrder != null)
            {
                PaymentAmountText = SelectedOrder.TongTien.ToString("0", CultureInfo.InvariantCulture);
            }
        }

        [RelayCommand]
        private void ConfirmPayment()
        {
            FeedbackMessage = string.Empty;

            if (SelectedOrder == null)
            {
                IsFeedbackSuccess = false;
                FeedbackMessage = "Vui lòng chọn một đơn hàng hợp lệ trước khi xác nhận thanh toán.";
                _dialogService.ShowError("Chưa chọn đơn hàng", FeedbackMessage);
                return;
            }

            var method = EnumExtensions.ParsePaymentMethod(SelectedPaymentMethodLabel);
            decimal? amount = null;

            if (IsAmountInputRequired)
            {
                if (!TryParsePaymentAmount(out decimal parsedAmount))
                {
                    IsFeedbackSuccess = false;
                    FeedbackMessage = "Số tiền thanh toán nhập vào không hợp lệ. Vui lòng chỉ nhập số dương (ví dụ: 250000).";
                    _dialogService.ShowError("Số tiền không hợp lệ", FeedbackMessage);
                    return;
                }
                amount = parsedAmount;
            }

            var result = _orderService.RecordPayment(
                SelectedOrder.MaDH,
                method,
                amount,
                ConfirmCodCollectionOnDelivery);

            if (!result.IsSuccess)
            {
                IsFeedbackSuccess = false;
                FeedbackMessage = result.Message;
                _dialogService.ShowError("Không thể ghi nhận thanh toán", result.Message);
                return;
            }

            IsFeedbackSuccess = true;
            FeedbackMessage = result.Message;
            OnPropertyChanged(nameof(SelectedOrder));
            OnPropertyChanged(nameof(CanShowCodDeliveryCheck));
            _dialogService.ShowInformation("Ghi nhận nghiệp vụ Bán hàng & Thanh toán", result.Message);
        }

        [RelayCommand]
        private void GoToOrderProcessing()
        {
            if (SelectedOrder == null)
            {
                return;
            }
            _navigationService.NavigateToStaffOrderProcessing(SelectedOrder.MaDH);
        }

        [RelayCommand]
        private void GoToStockOut()
        {
            if (SelectedOrder == null)
            {
                return;
            }
            _navigationService.NavigateToStaffStockOut(SelectedOrder.MaDH);
        }

        private bool TryParsePaymentAmount(out decimal amount)
        {
            amount = 0m;
            if (string.IsNullOrWhiteSpace(PaymentAmountText))
            {
                return false;
            }

            string cleaned = PaymentAmountText
                .Replace("₫", string.Empty)
                .Replace(",", string.Empty)
                .Replace(".", string.Empty)
                .Trim();

            return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
        }
    }
}
