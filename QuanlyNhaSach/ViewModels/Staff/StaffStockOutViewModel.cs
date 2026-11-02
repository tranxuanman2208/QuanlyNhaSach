using System;
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
    public partial class StaffStockOutViewModel : ObservableObject, INavigationAware
    {
        private readonly MockDataService _mockDataService;
        private readonly OrderService _orderService;
        private readonly InventoryService _inventoryService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;

        public ObservableCollection<Order> QuickOrdersList { get; } = new();
        public ObservableCollection<StockOutLineItem> StockOutItems { get; } = new();

        [ObservableProperty]
        private string _searchOrderIdText = string.Empty;

        private Order? _selectedOrder;
        public Order? SelectedOrder
        {
            get => _selectedOrder;
            set
            {
                if (SetProperty(ref _selectedOrder, value))
                {
                    if (_selectedOrder != null)
                    {
                        SearchOrderIdText = $"#{_selectedOrder.MaDH}";
                    }
                    LoadOrderStockOutItems();
                }
            }
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasSelectedOrder))]
        [NotifyPropertyChangedFor(nameof(IsNoOrderLoaded))]
        private bool _isOrderFound;

        [ObservableProperty]
        private bool _hasInsufficientStock;

        [ObservableProperty]
        private string _insufficientStockWarningMessage = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasFeedback))]
        private string _feedbackMessage = string.Empty;

        [ObservableProperty]
        private bool _isFeedbackSuccess = true;

        public bool HasSelectedOrder => IsOrderFound && SelectedOrder != null;
        public bool IsNoOrderLoaded => !HasSelectedOrder;
        public bool HasFeedback => !string.IsNullOrWhiteSpace(FeedbackMessage);

        public bool CanConfirmStockOut =>
            HasSelectedOrder &&
            SelectedOrder != null &&
            SelectedOrder.Status != OrderStatus.Cancelled &&
            SelectedOrder.StockOutStatus != StockOutStatus.Issued &&
            !HasInsufficientStock;

        public StaffStockOutViewModel(
            MockDataService mockDataService,
            OrderService orderService,
            InventoryService inventoryService,
            INavigationService navigationService,
            IDialogService dialogService)
        {
            _mockDataService = mockDataService ?? throw new ArgumentNullException(nameof(mockDataService));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            _mockDataService.DataChanged += (_, _) =>
            {
                RefreshQuickOrdersList();
                RefreshCurrentOrderItemsState();
            };

            RefreshQuickOrdersList();
            if (QuickOrdersList.Count > 0)
            {
                SelectedOrder = QuickOrdersList.FirstOrDefault(o => o.Status == OrderStatus.Confirmed && o.StockOutStatus != StockOutStatus.Issued)
                             ?? QuickOrdersList[0];
            }
        }

        public void OnNavigatedTo(object? parameter)
        {
            FeedbackMessage = string.Empty;
            RefreshQuickOrdersList();

            if (parameter is int orderId)
            {
                SearchOrderIdText = $"#{orderId}";
                SearchOrderById();
            }
            else if (SelectedOrder != null)
            {
                LoadOrderStockOutItems();
            }
        }

        private void RefreshQuickOrdersList()
        {
            int? currentId = _selectedOrder?.MaDH;
            var all = _orderService.GetAllOrders();
            QuickOrdersList.Clear();
            foreach (var order in all)
            {
                QuickOrdersList.Add(order);
            }

            if (currentId.HasValue)
            {
                _selectedOrder = QuickOrdersList.FirstOrDefault(o => o.MaDH == currentId.Value);
                OnPropertyChanged(nameof(SelectedOrder));
            }
        }

        [RelayCommand]
        private void SearchOrderById()
        {
            FeedbackMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(SearchOrderIdText))
            {
                IsFeedbackSuccess = false;
                FeedbackMessage = "Vui lòng nhập mã đơn hàng cần xuất kho (ví dụ: 1001, #1002 hoặc #1003).";
                return;
            }

            var foundOrder = _orderService.FindOrderByCodeOrId(SearchOrderIdText);
            if (foundOrder == null)
            {
                _selectedOrder = null;
                OnPropertyChanged(nameof(SelectedOrder));
                StockOutItems.Clear();
                IsOrderFound = false;
                HasInsufficientStock = false;
                InsufficientStockWarningMessage = string.Empty;
                IsFeedbackSuccess = false;
                FeedbackMessage = $"Không tìm thấy đơn hàng nào có mã \"{SearchOrderIdText.Trim()}\". Vui lòng kiểm tra lại.";
                OnPropertyChanged(nameof(CanConfirmStockOut));
                ConfirmStockOutCommand.NotifyCanExecuteChanged();
                return;
            }

            SelectedOrder = foundOrder;
        }

        private void LoadOrderStockOutItems()
        {
            StockOutItems.Clear();

            if (SelectedOrder == null)
            {
                IsOrderFound = false;
                HasInsufficientStock = false;
                InsufficientStockWarningMessage = string.Empty;
                OnPropertyChanged(nameof(CanConfirmStockOut));
                ConfirmStockOutCommand.NotifyCanExecuteChanged();
                return;
            }

            IsOrderFound = true;
            var lineItems = _inventoryService.GetStockOutLineItems(SelectedOrder);
            foreach (var line in lineItems)
            {
                StockOutItems.Add(line);
            }

            EvaluateStockOutState();
        }

        private void RefreshCurrentOrderItemsState()
        {
            if (SelectedOrder == null)
            {
                return;
            }

            foreach (var line in StockOutItems)
            {
                line.IsAlreadyIssued = SelectedOrder.StockOutStatus == StockOutStatus.Issued;
                line.RefreshStockState();
            }

            EvaluateStockOutState();
        }

        private void EvaluateStockOutState()
        {
            if (SelectedOrder == null)
            {
                HasInsufficientStock = false;
                InsufficientStockWarningMessage = string.Empty;
            }
            else if (SelectedOrder.StockOutStatus == StockOutStatus.Issued)
            {
                HasInsufficientStock = false;
                InsufficientStockWarningMessage = string.Empty;
            }
            else
            {
                var insufficientLines = StockOutItems.Where(x => x.IsInsufficientStock).ToList();
                HasInsufficientStock = insufficientLines.Count > 0;

                if (HasInsufficientStock)
                {
                    var details = insufficientLines.Select(
                        x => $"• Sách \"{x.TenSach}\" (Mã #{x.MaSach}): Cần xuất {x.IssueQuantity} cuốn nhưng tồn kho hiện chỉ còn {x.CurrentStock} cuốn (Thiếu {x.ShortageQuantity} cuốn).");

                    InsufficientStockWarningMessage =
                        $"CẢNH BÁO THIẾU TỒN KHO — Không thể xuất kho cho đơn hàng #{SelectedOrder.MaDH}:\n" +
                        string.Join("\n", details);
                }
                else
                {
                    InsufficientStockWarningMessage = string.Empty;
                }
            }

            OnPropertyChanged(nameof(CanConfirmStockOut));
            ConfirmStockOutCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private void ConfirmStockOut()
        {
            FeedbackMessage = string.Empty;

            if (SelectedOrder == null)
            {
                IsFeedbackSuccess = false;
                FeedbackMessage = "Vui lòng tìm kiếm và chọn một đơn hàng trước khi xác nhận xuất kho.";
                _dialogService.ShowError("Chưa chọn đơn hàng", FeedbackMessage);
                return;
            }

            var result = _inventoryService.ExecuteStockOut(SelectedOrder);
            if (!result.IsSuccess)
            {
                RefreshCurrentOrderItemsState();
                IsFeedbackSuccess = false;
                FeedbackMessage = result.Message;
                _dialogService.ShowError("Không thể xác nhận xuất kho", result.Message);
                return;
            }

            RefreshCurrentOrderItemsState();
            IsFeedbackSuccess = true;
            FeedbackMessage = result.Message;
            _dialogService.ShowInformation("Xác nhận xuất kho thành công", result.Message);
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
    }
}
