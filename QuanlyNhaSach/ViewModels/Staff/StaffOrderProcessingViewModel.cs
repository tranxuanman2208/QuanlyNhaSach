using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanlyNhaSach.Models;
using QuanlyNhaSach.Models.Enums;
using QuanlyNhaSach.Navigation;
using QuanlyNhaSach.Services;

namespace QuanlyNhaSach.ViewModels.Staff
{
    public partial class StaffOrderProcessingViewModel : ObservableObject, INavigationAware
    {
        private readonly OrderService _orderService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;

        public ObservableCollection<DONHANG> DisplayedOrders { get; } = new();
        public ObservableCollection<OrderSuggestionItem> OrderSuggestions { get; } = new();

        [ObservableProperty]
        private bool _isOrderSuggestionsOpen;

        public const string NeedsProcessingFilterLabel = "Đơn cần xử lý (Mới, Xác nhận, Đang giao)";
        public const string AllOrdersFilterLabel = "Tất cả đơn hàng";
        public const string HistoryCompletedFilterLabel = "Lịch sử đơn hàng (Đã hoàn tất)";

        public IReadOnlyList<string> StatusFilterOptions { get; } = new List<string>
        {
            NeedsProcessingFilterLabel,
            AllOrdersFilterLabel,
            HistoryCompletedFilterLabel,
            OrderStatus.New.ToVietnameseLabel(),
            OrderStatus.Confirmed.ToVietnameseLabel(),
            OrderStatus.Shipping.ToVietnameseLabel(),
            OrderStatus.Completed.ToVietnameseLabel(),
            OrderStatus.Cancelled.ToVietnameseLabel()
        };

        public IReadOnlyList<string> TransitionOptions { get; } = new List<string>
        {
            OrderStatus.New.ToVietnameseLabel(),
            OrderStatus.Confirmed.ToVietnameseLabel(),
            OrderStatus.Shipping.ToVietnameseLabel(),
            OrderStatus.Completed.ToVietnameseLabel(),
            OrderStatus.Cancelled.ToVietnameseLabel()
        };

        [ObservableProperty]
        private string _selectedStatusFilter = NeedsProcessingFilterLabel;

        [ObservableProperty]
        private string _tableTitle = "Danh Sách Đơn Hàng Cần Xử Lý";

        [ObservableProperty]
        private string _searchKeyword = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasSelectedOrder))]
        private DONHANG? _selectedOrder;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEmptyOrders))]
        private bool _hasOrders = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasFeedback))]
        private string _feedbackMessage = string.Empty;

        [ObservableProperty]
        private bool _isFeedbackSuccess = true;

        public bool HasSelectedOrder => SelectedOrder != null;
        public bool IsEmptyOrders => !HasOrders;
        public bool HasFeedback => !string.IsNullOrWhiteSpace(FeedbackMessage);

        public StaffOrderProcessingViewModel(
            OrderService orderService,
            INavigationService navigationService,
            IDialogService dialogService)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            _orderService.OrderUpdated += (_, _) => RefreshOrders(preserveSelection: true);
            RefreshOrders();
        }

        partial void OnSelectedStatusFilterChanged(string value)
        {
            RefreshOrders(preserveSelection: true);
        }

        partial void OnSearchKeywordChanged(string value)
        {
            UpdateOrderSuggestions(value);
            RefreshOrders(preserveSelection: true);
        }

        private void UpdateOrderSuggestions(string keyword)
        {
            OrderSuggestions.Clear();
            if (string.IsNullOrWhiteSpace(keyword))
            {
                IsOrderSuggestionsOpen = false;
                return;
            }

            string kw = keyword.Trim().TrimStart('#');
            var matches = _orderService.GetAllOrders()
                .Where(o =>
                    o.MaDH.ToString().Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    o.OrderCode.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    o.HoTenKhachHang.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    o.SDTKhachHang.Contains(kw, StringComparison.OrdinalIgnoreCase))
                .Take(5)
                .Select(o => new OrderSuggestionItem
                {
                    MaDH = o.MaDH,
                    OrderCode = o.OrderCode,
                    CustomerInfo = string.IsNullOrWhiteSpace(o.SDTKhachHang) ? o.HoTenKhachHang : $"{o.HoTenKhachHang} ({o.SDTKhachHang})",
                    Status = o.TrangThai
                })
                .ToList();

            foreach (var item in matches)
            {
                OrderSuggestions.Add(item);
            }

            IsOrderSuggestionsOpen = OrderSuggestions.Count > 0;
        }

        [RelayCommand]
        public void SelectOrderSuggestion(OrderSuggestionItem item)
        {
            if (item == null) return;
            SearchKeyword = item.OrderCode;
            IsOrderSuggestionsOpen = false;
            RefreshOrders(preserveSelection: false);
        }

        [RelayCommand]
        public void CloseOrderSuggestions()
        {
            IsOrderSuggestionsOpen = false;
        }

        public void OnNavigatedTo(object? parameter)
        {
            FeedbackMessage = string.Empty;
            _orderService.InvalidateCache();

            if (parameter is int targetOrderId)
            {
                SelectedStatusFilter = AllOrdersFilterLabel;
                SearchKeyword = string.Empty;
                RefreshOrders(preserveSelection: false);
                SelectedOrder = DisplayedOrders.FirstOrDefault(o => o.MaDH == targetOrderId);
            }
            else
            {
                SelectedStatusFilter = NeedsProcessingFilterLabel;
                RefreshOrders(preserveSelection: true);
            }
        }

        [RelayCommand]
        public void RefreshOrders(bool preserveSelection = true)
        {
            int? previousId = preserveSelection ? SelectedOrder?.MaDH : null;

            _orderService.InvalidateCache();
            IEnumerable<DONHANG> filtered;
            if (SelectedStatusFilter == NeedsProcessingFilterLabel)
            {
                filtered = _orderService.GetAllOrders()
                    .Where(o => o.Status == OrderStatus.New || o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Shipping);
                TableTitle = "Danh Sách Đơn Hàng Cần Xử Lý";
            }
            else if (SelectedStatusFilter == AllOrdersFilterLabel || SelectedStatusFilter == OrderService.AllFilterLabel)
            {
                filtered = _orderService.GetAllOrders();
                TableTitle = "Danh Sách Tất Cả Đơn Hàng";
            }
            else if (SelectedStatusFilter == HistoryCompletedFilterLabel || SelectedStatusFilter == OrderStatus.Completed.ToVietnameseLabel())
            {
                filtered = _orderService.GetAllOrders().Where(o => o.Status == OrderStatus.Completed);
                TableTitle = "Lịch Sử Đơn Hàng Đã Hoàn Tất";
            }
            else if (SelectedStatusFilter == OrderStatus.New.ToVietnameseLabel())
            {
                filtered = _orderService.GetAllOrders().Where(o => o.Status == OrderStatus.New);
                TableTitle = "Danh Sách Đơn Hàng Mới";
            }
            else if (SelectedStatusFilter == OrderStatus.Confirmed.ToVietnameseLabel())
            {
                filtered = _orderService.GetAllOrders().Where(o => o.Status == OrderStatus.Confirmed);
                TableTitle = "Danh Sách Đơn Hàng Đã Xác Nhận";
            }
            else if (SelectedStatusFilter == OrderStatus.Shipping.ToVietnameseLabel())
            {
                filtered = _orderService.GetAllOrders().Where(o => o.Status == OrderStatus.Shipping);
                TableTitle = "Danh Sách Đơn Hàng Đang Giao";
            }
            else if (SelectedStatusFilter == OrderStatus.Cancelled.ToVietnameseLabel())
            {
                filtered = _orderService.GetAllOrders().Where(o => o.Status == OrderStatus.Cancelled);
                TableTitle = "Danh Sách Đơn Hàng Đã Hủy";
            }
            else
            {
                filtered = _orderService.GetOrdersByStatus(SelectedStatusFilter);
                TableTitle = $"Danh Sách Đơn Hàng ({SelectedStatusFilter})";
            }

            if (!string.IsNullOrWhiteSpace(SearchKeyword))
            {
                string kw = SearchKeyword.Trim().TrimStart('#');
                filtered = filtered.Where(o =>
                    o.MaDH.ToString().Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    o.OrderCode.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    o.HoTenKhachHang.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    o.SDTKhachHang.Contains(kw, StringComparison.OrdinalIgnoreCase));
            }

            DisplayedOrders.Clear();
            foreach (var order in filtered)
            {
                DisplayedOrders.Add(order);
            }

            HasOrders = DisplayedOrders.Count > 0;

            if (previousId.HasValue)
            {
                SelectedOrder = DisplayedOrders.FirstOrDefault(o => o.MaDH == previousId.Value)
                             ?? DisplayedOrders.FirstOrDefault();
            }
            else if (SelectedOrder == null && DisplayedOrders.Count > 0)
            {
                SelectedOrder = DisplayedOrders[0];
            }
        }

        [RelayCommand]
        public void ViewAllOrders()
        {
            SelectedStatusFilter = AllOrdersFilterLabel;
            SearchKeyword = string.Empty;
            FeedbackMessage = string.Empty;
            RefreshOrders(preserveSelection: false);
        }

        [RelayCommand]
        public void ViewOrderHistory()
        {
            SelectedStatusFilter = HistoryCompletedFilterLabel;
            SearchKeyword = string.Empty;
            FeedbackMessage = string.Empty;
            RefreshOrders(preserveSelection: false);
        }

        [RelayCommand]
        public void FilterNeedsProcessing()
        {
            SelectedStatusFilter = NeedsProcessingFilterLabel;
            SearchKeyword = string.Empty;
            FeedbackMessage = string.Empty;
            RefreshOrders(preserveSelection: false);
        }

        [RelayCommand]
        public void FilterNewOrders()
        {
            SelectedStatusFilter = OrderStatus.New.ToVietnameseLabel();
            SearchKeyword = string.Empty;
            FeedbackMessage = string.Empty;
            RefreshOrders(preserveSelection: false);
        }

        [RelayCommand]
        public void ResetFilters()
        {
            SelectedStatusFilter = NeedsProcessingFilterLabel;
            SearchKeyword = string.Empty;
            FeedbackMessage = string.Empty;
            RefreshOrders(preserveSelection: false);
        }

        [RelayCommand]
        private void ConfirmBankTransferPayment(DONHANG? order)
        {
            var targetOrder = order ?? SelectedOrder;
            if (targetOrder == null)
            {
                _dialogService.ShowError("Chưa chọn đơn hàng", "Vui lòng chọn đơn hàng cần duyệt thanh toán chuyển khoản.");
                return;
            }

            bool confirmed = _dialogService.ShowConfirmation(
                "Xác nhận đã nhận tiền chuyển khoản",
                $"Bạn đã kiểm tra tài khoản ngân hàng và nhận đủ {targetOrder.FormattedTongTien} cho đơn hàng {targetOrder.OrderCode} ({targetOrder.CustomerSummary})?\n\n" +
                $"Hệ thống sẽ đánh dấu Đã thanh toán và tự động chuyển đơn sang trạng thái 'Đã xác nhận'.");

            if (!confirmed)
            {
                return;
            }

            var result = _orderService.ConfirmBankTransferPayment(targetOrder.MaDH);
            if (!result.IsSuccess)
            {
                IsFeedbackSuccess = false;
                FeedbackMessage = result.Message;
                _dialogService.ShowError("Không thể duyệt thanh toán", result.Message);
                return;
            }

            IsFeedbackSuccess = true;
            FeedbackMessage = result.Message;
            SelectedOrder = result.Data ?? targetOrder;
            RefreshOrders(preserveSelection: true);
            _dialogService.ShowInformation("Duyệt chuyển khoản & Xác nhận đơn thành công", result.Message);
        }

        [RelayCommand]
        private void ApplyStatusTransition(DONHANG? order)
        {
            var targetOrder = order ?? SelectedOrder;
            if (targetOrder == null)
            {
                _dialogService.ShowError("Chưa chọn đơn hàng", "Vui lòng chọn một đơn hàng cần cập nhật trạng thái.");
                return;
            }

            var result = _orderService.TransitionOrderStatusByLabel(
                targetOrder.MaDH,
                targetOrder.SelectedTransitionLabel);

            if (!result.IsSuccess)
            {
                IsFeedbackSuccess = false;
                FeedbackMessage = result.Message;
                _dialogService.ShowError("Không thể chuyển trạng thái đơn hàng", result.Message);
                return;
            }

            IsFeedbackSuccess = true;
            FeedbackMessage = result.Message;
            SelectedOrder = result.Data ?? targetOrder;
            RefreshOrders(preserveSelection: true);
            _dialogService.ShowInformation("Cập nhật trạng thái thành công", result.Message);
        }

        [RelayCommand]
        private void QuickConfirmOrder(DONHANG? order)
        {
            var targetOrder = order ?? SelectedOrder;
            if (targetOrder == null)
            {
                return;
            }

            if (targetOrder.IsAwaitingBankTransferConfirmation)
            {
                ConfirmBankTransferPayment(targetOrder);
                return;
            }

            ExecuteDirectTransition(targetOrder, OrderStatus.Confirmed);
        }

        [RelayCommand]
        private void QuickShipOrder(DONHANG? order)
        {
            var targetOrder = order ?? SelectedOrder;
            if (targetOrder == null)
            {
                return;
            }

            ExecuteDirectTransition(targetOrder, OrderStatus.Shipping);
        }

        [RelayCommand]
        private void QuickCompleteOrder(DONHANG? order)
        {
            var targetOrder = order ?? SelectedOrder;
            if (targetOrder == null)
            {
                return;
            }

            ExecuteDirectTransition(targetOrder, OrderStatus.Completed);
        }

        private void ExecuteDirectTransition(DONHANG targetOrder, OrderStatus nextStatus)
        {
            var result = _orderService.TransitionOrderStatus(targetOrder.MaDH, nextStatus);
            if (!result.IsSuccess)
            {
                IsFeedbackSuccess = false;
                FeedbackMessage = result.Message;
                _dialogService.ShowError("Thao tác không hợp lệ", result.Message);
                return;
            }

            IsFeedbackSuccess = true;
            FeedbackMessage = result.Message;
            SelectedOrder = result.Data ?? targetOrder;
            RefreshOrders(preserveSelection: true);
            _dialogService.ShowInformation("Cập nhật thành công", result.Message);
        }

        [RelayCommand]
        private void OpenStockOut(DONHANG? order)
        {
            var target = order ?? SelectedOrder;
            if (target == null)
            {
                return;
            }
            _navigationService.NavigateToStaffStockOut(target.MaDH);
        }

        [RelayCommand]
        private void OpenSales(DONHANG? order)
        {
            var target = order ?? SelectedOrder;
            if (target == null)
            {
                return;
            }
            _navigationService.NavigateToStaffSales(target.MaDH);
        }
    }

    public class OrderSuggestionItem
    {
        public int MaDH { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string CustomerInfo { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
