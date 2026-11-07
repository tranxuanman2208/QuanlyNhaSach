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
        private readonly OrderService _orderService;
        private readonly InventoryService _inventoryService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;

        public ObservableCollection<DONHANG> QuickOrdersList { get; } = new();
        public ObservableCollection<StockOutLineItem> StockOutItems { get; } = new();

        [ObservableProperty]
        private string _searchOrderIdText = string.Empty;

        private DONHANG? _selectedOrder;

        // Cờ chặn ComboBox đẩy null về SelectedOrder khi QuickOrdersList đang được làm mới (Clear/Add)
        private bool _isRefreshingQuickOrders;

        public DONHANG? SelectedOrder
        {
            get => _selectedOrder;
            set
            {
                if (_isRefreshingQuickOrders)
                {
                    return;
                }

                SetSelectedOrderAndLoad(value, forceReload: false);
            }
        }

        private void SetSelectedOrderAndLoad(DONHANG? order, bool forceReload)
        {
            bool changed = SetProperty(ref _selectedOrder, order, nameof(SelectedOrder));
            if (!changed && !forceReload)
            {
                return;
            }

            if (_selectedOrder != null)
            {
                SearchOrderIdText = $"#{_selectedOrder.MaDH}";
            }

            LoadOrderStockOutItems();
            NotifySelectionStateChanged();
        }

        private void NotifySelectionStateChanged()
        {
            OnPropertyChanged(nameof(HasSelectedOrder));
            OnPropertyChanged(nameof(IsNoOrderLoaded));
            OnPropertyChanged(nameof(CanConfirmStockOut));
            ConfirmStockOutCommand.NotifyCanExecuteChanged();
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

        public bool HasSelectedOrder => SelectedOrder != null;
        public bool IsNoOrderLoaded => !HasSelectedOrder;
        public bool HasFeedback => !string.IsNullOrWhiteSpace(FeedbackMessage);

        public bool CanConfirmStockOut =>
            HasSelectedOrder &&
            SelectedOrder != null &&
            SelectedOrder.Status != OrderStatus.Cancelled &&
            SelectedOrder.StockOutStatus != StockOutStatus.Issued &&
            !HasInsufficientStock;

        public StaffStockOutViewModel(
            OrderService orderService,
            InventoryService inventoryService,
            INavigationService navigationService,
            IDialogService dialogService)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            _orderService.OrderUpdated += (_, _) =>
            {
                RefreshQuickOrdersList();
                RefreshCurrentOrderItemsState();
            };
            _inventoryService.InventoryChanged += (_, _) =>
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

            _isRefreshingQuickOrders = true;
            try
            {
                QuickOrdersList.Clear();
                foreach (var order in all)
                {
                    QuickOrdersList.Add(order);
                }

                if (currentId.HasValue)
                {
                    // Dùng instance mới nhất trong danh sách để ComboBox chọn đúng phần tử
                    _selectedOrder = QuickOrdersList.FirstOrDefault(o => o.MaDH == currentId.Value) ?? _selectedOrder;
                }
            }
            finally
            {
                _isRefreshingQuickOrders = false;
            }

            OnPropertyChanged(nameof(SelectedOrder));
            NotifySelectionStateChanged();
        }

        [RelayCommand]
        private void SearchOrderById()
        {
            FeedbackMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(SearchOrderIdText))
            {
                IsFeedbackSuccess = false;
                FeedbackMessage = "Vui lòng nhập mã đơn hàng cần xuất kho (ví dụ: 1, #DH0001).";
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
                NotifySelectionStateChanged();
                return;
            }

            // Ưu tiên instance có trong QuickOrdersList để ComboBox hiển thị đồng bộ;
            // luôn tải lại chi tiết kể cả khi đơn tìm được trùng với đơn đang chọn.
            var orderInList = QuickOrdersList.FirstOrDefault(o => o.MaDH == foundOrder.MaDH) ?? foundOrder;
            SetSelectedOrderAndLoad(orderInList, forceReload: true);
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

            LoadOrderStockOutItems();
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

            if (result.Data != null)
            {
                _selectedOrder = result.Data;
                OnPropertyChanged(nameof(SelectedOrder));
            }

            RefreshQuickOrdersList();
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
