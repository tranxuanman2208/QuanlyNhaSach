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
        private readonly MockDataService _mockDataService;
        private readonly OrderService _orderService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;

        public ObservableCollection<Order> DisplayedOrders { get; } = new();

        public IReadOnlyList<string> StatusFilterOptions { get; } = new List<string>
        {
            OrderService.AllFilterLabel,
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
        private string _selectedStatusFilter = OrderService.AllFilterLabel;

        [ObservableProperty]
        private string _searchKeyword = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasSelectedOrder))]
        private Order? _selectedOrder;

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
            MockDataService mockDataService,
            OrderService orderService,
            INavigationService navigationService,
            IDialogService dialogService)
        {
            _mockDataService = mockDataService ?? throw new ArgumentNullException(nameof(mockDataService));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            _mockDataService.DataChanged += (_, _) => RefreshOrders(preserveSelection: true);
            RefreshOrders();
        }

        partial void OnSelectedStatusFilterChanged(string value)
        {
            RefreshOrders(preserveSelection: true);
        }

        partial void OnSearchKeywordChanged(string value)
        {
            RefreshOrders(preserveSelection: true);
        }

        public void OnNavigatedTo(object? parameter)
        {
            FeedbackMessage = string.Empty;

            if (parameter is int targetOrderId)
            {
                SelectedStatusFilter = OrderService.AllFilterLabel;
                SearchKeyword = string.Empty;
                RefreshOrders(preserveSelection: false);
                SelectedOrder = DisplayedOrders.FirstOrDefault(o => o.MaDH == targetOrderId);
            }
            else
            {
                RefreshOrders(preserveSelection: true);
            }
        }

        [RelayCommand]
        public void RefreshOrders(bool preserveSelection = true)
        {
            int? previousId = preserveSelection ? SelectedOrder?.MaDH : null;

            IEnumerable<Order> filtered = _orderService.GetOrdersByStatus(SelectedStatusFilter);

            if (!string.IsNullOrWhiteSpace(SearchKeyword))
            {
                string kw = SearchKeyword.Trim().TrimStart('#');
                filtered = filtered.Where(o =>
                    o.MaDH.ToString().Contains(kw, StringComparison.OrdinalIgnoreCase) ||
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
        private void ResetFilters()
        {
            SelectedStatusFilter = OrderService.AllFilterLabel;
            SearchKeyword = string.Empty;
            FeedbackMessage = string.Empty;
            RefreshOrders();
        }

        [RelayCommand]
        private void ApplyStatusTransition(Order? order)
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
            SelectedOrder = targetOrder;
            RefreshOrders(preserveSelection: true);
            _dialogService.ShowInformation("Cập nhật trạng thái thành công", result.Message);
        }

        [RelayCommand]
        private void QuickConfirmOrder(Order? order)
        {
            var targetOrder = order ?? SelectedOrder;
            if (targetOrder == null)
            {
                return;
            }

            ExecuteDirectTransition(targetOrder, OrderStatus.Confirmed);
        }

        [RelayCommand]
        private void QuickShipOrder(Order? order)
        {
            var targetOrder = order ?? SelectedOrder;
            if (targetOrder == null)
            {
                return;
            }

            ExecuteDirectTransition(targetOrder, OrderStatus.Shipping);
        }

        [RelayCommand]
        private void QuickCompleteOrder(Order? order)
        {
            var targetOrder = order ?? SelectedOrder;
            if (targetOrder == null)
            {
                return;
            }

            ExecuteDirectTransition(targetOrder, OrderStatus.Completed);
        }

        private void ExecuteDirectTransition(Order targetOrder, OrderStatus nextStatus)
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
            SelectedOrder = targetOrder;
            RefreshOrders(preserveSelection: true);
            _dialogService.ShowInformation("Cập nhật thành công", result.Message);
        }

        [RelayCommand]
        private void OpenStockOut(Order? order)
        {
            var target = order ?? SelectedOrder;
            if (target == null)
            {
                return;
            }
            _navigationService.NavigateToStaffStockOut(target.MaDH);
        }

        [RelayCommand]
        private void OpenSales(Order? order)
        {
            var target = order ?? SelectedOrder;
            if (target == null)
            {
                return;
            }
            _navigationService.NavigateToStaffSales(target.MaDH);
        }
    }
}
