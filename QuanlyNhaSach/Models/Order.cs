using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using QuanlyNhaSach.Models.Enums;

namespace QuanlyNhaSach.Models
{
    public partial class Order : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(OrderCode))]
        [NotifyPropertyChangedFor(nameof(DisplayTitle))]
        private int _maDH;

        [ObservableProperty]
        private int _maND;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CustomerSummary))]
        [NotifyPropertyChangedFor(nameof(DisplayTitle))]
        private string _hoTenKhachHang = string.Empty;

        private string _sdtKhachHang = string.Empty;
        public string SDTKhachHang
        {
            get => _sdtKhachHang;
            set
            {
                if (SetProperty(ref _sdtKhachHang, value))
                {
                    OnPropertyChanged(nameof(CustomerSummary));
                }
            }
        }

        [ObservableProperty]
        private string _diaChiGiaoHang = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FormattedNgayDat))]
        private DateTime _ngayDat = DateTime.Now;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FormattedNgayCapNhat))]
        private DateTime _ngayCapNhat = DateTime.Now;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FormattedTongTien))]
        [NotifyPropertyChangedFor(nameof(DisplayTitle))]
        private decimal _tongTien;

        private PaymentMethod _method = PaymentMethod.COD;
        public PaymentMethod Method
        {
            get => _method;
            set
            {
                if (SetProperty(ref _method, value))
                {
                    _phuongThucThanhToan = value.ToVietnameseLabel();
                    OnPropertyChanged(nameof(PhuongThucThanhToan));
                    OnPropertyChanged(nameof(RequiredActionText));
                }
            }
        }

        private string _phuongThucThanhToan = PaymentMethod.COD.ToVietnameseLabel();
        public string PhuongThucThanhToan
        {
            get => _phuongThucThanhToan;
            set
            {
                var parsed = EnumExtensions.ParsePaymentMethod(value);
                _method = parsed;
                string normalized = parsed.ToVietnameseLabel();
                if (SetProperty(ref _phuongThucThanhToan, normalized))
                {
                    OnPropertyChanged(nameof(Method));
                    OnPropertyChanged(nameof(RequiredActionText));
                }
            }
        }

        private OrderStatus _status = OrderStatus.New;
        public OrderStatus Status
        {
            get => _status;
            set
            {
                if (SetProperty(ref _status, value))
                {
                    _trangThai = value.ToVietnameseLabel();
                    SyncDefaultSelectedTransition();
                    OnPropertyChanged(nameof(TrangThai));
                    OnPropertyChanged(nameof(RequiredActionText));
                    OnPropertyChanged(nameof(IsUrgent));
                    OnPropertyChanged(nameof(CanTransitionStatus));
                    OnPropertyChanged(nameof(CanRecordPayment));
                    OnPropertyChanged(nameof(CanExecuteStockOut));
                    OnPropertyChanged(nameof(DisplayTitle));
                }
            }
        }

        private string _trangThai = OrderStatus.New.ToVietnameseLabel();
        public string TrangThai
        {
            get => _trangThai;
            set
            {
                if (EnumExtensions.TryParseOrderStatusLabel(value, out var parsed))
                {
                    _status = parsed;
                    string label = parsed.ToVietnameseLabel();
                    if (SetProperty(ref _trangThai, label))
                    {
                        SyncDefaultSelectedTransition();
                        OnPropertyChanged(nameof(Status));
                        OnPropertyChanged(nameof(RequiredActionText));
                        OnPropertyChanged(nameof(IsUrgent));
                        OnPropertyChanged(nameof(CanTransitionStatus));
                        OnPropertyChanged(nameof(CanRecordPayment));
                        OnPropertyChanged(nameof(CanExecuteStockOut));
                        OnPropertyChanged(nameof(DisplayTitle));
                    }
                }
                else
                {
                    SetProperty(ref _trangThai, value);
                }
            }
        }

        private PaymentStatus _paymentStatus = PaymentStatus.Pending;
        public PaymentStatus PaymentStatus
        {
            get => _paymentStatus;
            set
            {
                if (SetProperty(ref _paymentStatus, value))
                {
                    OnPropertyChanged(nameof(TrangThaiThanhToan));
                    OnPropertyChanged(nameof(IsPaid));
                    OnPropertyChanged(nameof(CanRecordPayment));
                    OnPropertyChanged(nameof(RequiredActionText));
                }
            }
        }

        private StockOutStatus _stockOutStatus = StockOutStatus.NotIssued;
        public StockOutStatus StockOutStatus
        {
            get => _stockOutStatus;
            set
            {
                if (SetProperty(ref _stockOutStatus, value))
                {
                    OnPropertyChanged(nameof(TrangThaiXuatKho));
                    OnPropertyChanged(nameof(IsStockIssued));
                    OnPropertyChanged(nameof(CanExecuteStockOut));
                    OnPropertyChanged(nameof(RequiredActionText));
                }
            }
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FormattedPaidAmount))]
        private decimal _paidAmount;

        [ObservableProperty]
        private string _paymentNote = string.Empty;

        [ObservableProperty]
        private DateTime? _stockOutDate;

        private string _selectedTransitionLabel = "Đã xác nhận";
        public string SelectedTransitionLabel
        {
            get => _selectedTransitionLabel;
            set => SetProperty(ref _selectedTransitionLabel, value);
        }

        private IReadOnlyList<OrderItem> _items = Array.Empty<OrderItem>();
        public IReadOnlyList<OrderItem> Items
        {
            get => _items;
            set
            {
                if (SetProperty(ref _items, value ?? Array.Empty<OrderItem>()))
                {
                    OnPropertyChanged(nameof(TotalQuantity));
                    OnPropertyChanged(nameof(BooksSummary));
                }
            }
        }

        public Order()
        {
            SyncDefaultSelectedTransition();
        }

        private void SyncDefaultSelectedTransition()
        {
            _selectedTransitionLabel = _status switch
            {
                OrderStatus.New => OrderStatus.Confirmed.ToVietnameseLabel(),
                OrderStatus.Confirmed => OrderStatus.Shipping.ToVietnameseLabel(),
                OrderStatus.Shipping => OrderStatus.Completed.ToVietnameseLabel(),
                OrderStatus.Completed => OrderStatus.Completed.ToVietnameseLabel(),
                OrderStatus.Cancelled => OrderStatus.Cancelled.ToVietnameseLabel(),
                _ => OrderStatus.Confirmed.ToVietnameseLabel()
            };
            OnPropertyChanged(nameof(SelectedTransitionLabel));
        }

        public string OrderCode => $"#{MaDH}";
        public string DisplayTitle => $"#{MaDH} — {HoTenKhachHang} ({FormattedTongTien} | {TrangThai})";
        public string CustomerSummary => string.IsNullOrWhiteSpace(SDTKhachHang)
            ? HoTenKhachHang
            : $"{HoTenKhachHang} ({SDTKhachHang})";

        public string TrangThaiThanhToan => PaymentStatus.ToVietnameseLabel();
        public string TrangThaiXuatKho => StockOutStatus.ToVietnameseLabel();

        public bool IsPaid => PaymentStatus == PaymentStatus.Paid;
        public bool IsStockIssued => StockOutStatus == StockOutStatus.Issued;
        public bool CanTransitionStatus => Status != OrderStatus.Completed && Status != OrderStatus.Cancelled;
        public bool CanRecordPayment => Status != OrderStatus.Cancelled && PaymentStatus != PaymentStatus.Paid;
        public bool CanExecuteStockOut => Status != OrderStatus.Cancelled && Status != OrderStatus.Completed && StockOutStatus != StockOutStatus.Issued;

        public bool IsUrgent =>
            Status == OrderStatus.New ||
            Status == OrderStatus.Confirmed ||
            (Status == OrderStatus.Shipping && PaymentStatus != PaymentStatus.Paid);

        public int TotalQuantity => Items?.Sum(i => i.SoLuongMua) ?? 0;

        public string BooksSummary =>
            Items == null || Items.Count == 0
                ? "Chưa có sách"
                : string.Join(", ", Items.Select(i => $"{i.TenSach} (x{i.SoLuongMua})"));

        public string RequiredActionText
        {
            get
            {
                return Status switch
                {
                    OrderStatus.New => "Cần xác nhận đơn hàng mới",
                    OrderStatus.Confirmed when StockOutStatus == StockOutStatus.InsufficientStock
                        => "Thiếu tồn kho — Cần kiểm tra xuất kho",
                    OrderStatus.Confirmed when StockOutStatus != StockOutStatus.Issued
                        => "Cần xuất kho sách để giao hàng",
                    OrderStatus.Confirmed when StockOutStatus == StockOutStatus.Issued
                        => "Đã xuất kho — Sẵn sàng chuyển Đang giao",
                    OrderStatus.Shipping when PaymentStatus != PaymentStatus.Paid
                        => Method == PaymentMethod.COD
                            ? "Đang giao — Chờ thu hộ COD & hoàn tất"
                            : "Đang giao — Cần xác nhận thanh toán",
                    OrderStatus.Shipping when PaymentStatus == PaymentStatus.Paid
                        => "Đã thanh toán — Chờ xác nhận Hoàn tất",
                    OrderStatus.Completed => "Đã hoàn tất",
                    OrderStatus.Cancelled => "Đơn hàng đã hủy",
                    _ => "Cần kiểm tra"
                };
            }
        }

        public string FormattedTongTien => $"{TongTien:N0} ₫";
        public string FormattedPaidAmount => $"{PaidAmount:N0} ₫";
        public string FormattedNgayDat => NgayDat.ToString("dd/MM/yyyy HH:mm");
        public string FormattedNgayCapNhat => NgayCapNhat.ToString("dd/MM/yyyy HH:mm");
    }
}
