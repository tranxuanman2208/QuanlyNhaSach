using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using QuanlyNhaSach.Models.Enums;

namespace QuanlyNhaSach.Models
{
    public partial class DONHANG : ObservableObject
    {
        private string? _hoTenKhachHang;
        private string? _sdtKhachHang;
        private string? _diaChiGiaoHang;
        private DateTime? _ngayDat;
        private PaymentStatus? _paymentStatusOverride;
        private StockOutStatus? _stockOutStatusOverride;
        private string? _selectedTransitionLabel;

        [NotMapped]
        public string OrderCode => $"#DH{MaDH:D4}";

        [NotMapped]
        public string HoTenKhachHang
        {
            get => !string.IsNullOrWhiteSpace(_hoTenKhachHang)
                ? _hoTenKhachHang
                : (MaKHNavigation?.Hoten ?? "Khách vãng lai");
            set => SetProperty(ref _hoTenKhachHang, value);
        }

        [NotMapped]
        public string SDTKhachHang
        {
            get => !string.IsNullOrWhiteSpace(_sdtKhachHang)
                ? _sdtKhachHang
                : (MaKHNavigation?.SDT ?? "N/A");
            set => SetProperty(ref _sdtKhachHang, value);
        }

        [NotMapped]
        public string DiaChiGiaoHang
        {
            get => !string.IsNullOrWhiteSpace(_diaChiGiaoHang)
                ? _diaChiGiaoHang
                : (MaKHNavigation?.Diachi ?? "Nhận tại nhà sách / Chưa cập nhật");
            set => SetProperty(ref _diaChiGiaoHang, value);
        }

        [NotMapped]
        public string CustomerSummary => $"{HoTenKhachHang} ({SDTKhachHang})";

        [NotMapped]
        public string DisplayTitle => $"{OrderCode} — {HoTenKhachHang} ({FormattedTongTien})";

        [NotMapped]
        public DateTime NgayDat
        {
            get => _ngayDat ?? NgayTao.ToDateTime(TimeOnly.MinValue);
            set
            {
                _ngayDat = value;
                NgayTao = DateOnly.FromDateTime(value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(FormattedNgayDat));
            }
        }

        [NotMapped]
        public decimal TongTien
        {
            get => Tongtien ?? (CHITIETDONHANGs?.Sum(i => i.ThanhTien) ?? 0m);
            set
            {
                Tongtien = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Tongtien));
                OnPropertyChanged(nameof(FormattedTongTien));
                OnPropertyChanged(nameof(DisplayTitle));
            }
        }

        [NotMapped]
        public OrderStatus Status
        {
            get => EnumExtensions.TryParseOrderStatusLabel(Trangthaigiaohang, out var parsed)
                ? parsed
                : OrderStatus.New;
            set
            {
                Trangthaigiaohang = value.ToVietnameseLabel();
                if (_paymentStatusOverride == PaymentStatus.Refunded && value != OrderStatus.Cancelled)
                {
                    _paymentStatusOverride = null;
                }
                else if (_paymentStatusOverride == PaymentStatus.Paid && value == OrderStatus.Cancelled)
                {
                    _paymentStatusOverride = PaymentStatus.Refunded;
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(Trangthaigiaohang));
                OnPropertyChanged(nameof(TrangThai));
                OnPropertyChanged(nameof(PaymentStatus));
                OnPropertyChanged(nameof(TrangThaiThanhToan));
                OnPropertyChanged(nameof(CanRecordPayment));
                OnPropertyChanged(nameof(IsAwaitingBankTransferConfirmation));
                OnPropertyChanged(nameof(CanTransitionStatus));
                OnPropertyChanged(nameof(RequiredActionText));
            }
        }

        [NotMapped]
        public PaymentMethod Method
        {
            get => EnumExtensions.ParsePaymentMethod(Phuongthucthanhtoan);
            set
            {
                // Store ASCII code in Phuongthucthanhtoan (varchar(100) in SQL Server) to avoid losing diacritics
                Phuongthucthanhtoan = value switch
                {
                    PaymentMethod.Cash => "Cash",
                    PaymentMethod.BankTransfer => "BankTransfer",
                    _ => "COD"
                };
                OnPropertyChanged();
                OnPropertyChanged(nameof(Phuongthucthanhtoan));
                OnPropertyChanged(nameof(PhuongThucThanhToan));
                OnPropertyChanged(nameof(PaymentStatus));
                OnPropertyChanged(nameof(TrangThaiThanhToan));
                OnPropertyChanged(nameof(IsAwaitingBankTransferConfirmation));
                OnPropertyChanged(nameof(RequiredActionText));
            }
        }

        [NotMapped]
        public PaymentStatus PaymentStatus
        {
            get
            {
                if (_paymentStatusOverride.HasValue)
                {
                    return _paymentStatusOverride.Value;
                }

                if (Dathanhtoan == true)
                {
                    return Status == OrderStatus.Cancelled
                        ? PaymentStatus.Refunded
                        : PaymentStatus.Paid;
                }

                if (Method == PaymentMethod.COD && Status != OrderStatus.Cancelled)
                {
                    return PaymentStatus.PayOnDelivery;
                }
                return PaymentStatus.Pending;
            }
            set
            {
                _paymentStatusOverride = value;
                Dathanhtoan = (value == PaymentStatus.Paid || value == PaymentStatus.Refunded);
                OnPropertyChanged();
                OnPropertyChanged(nameof(Dathanhtoan));
                OnPropertyChanged(nameof(TrangThaiThanhToan));
                OnPropertyChanged(nameof(CanRecordPayment));
                OnPropertyChanged(nameof(IsAwaitingBankTransferConfirmation));
                OnPropertyChanged(nameof(RequiredActionText));
            }
        }

        [NotMapped]
        public StockOutStatus StockOutStatus
        {
            get
            {
                if (_stockOutStatusOverride.HasValue)
                {
                    return _stockOutStatusOverride.Value;
                }

                if (PHIEUXUATs != null && PHIEUXUATs.Count > 0)
                {
                    return StockOutStatus.Issued;
                }

                if (Status == OrderStatus.Shipping || Status == OrderStatus.Completed)
                {
                    return StockOutStatus.Issued;
                }

                return StockOutStatus.NotIssued;
            }
            set
            {
                _stockOutStatusOverride = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TrangThaiXuatKho));
                OnPropertyChanged(nameof(RequiredActionText));
            }
        }

        [NotMapped]
        public IReadOnlyList<CHITIETDONHANG> Items => CHITIETDONHANGs?.ToList() ?? new List<CHITIETDONHANG>();

        [NotMapped]
        public int TotalQuantity => CHITIETDONHANGs?.Sum(i => i.SoLuongMua) ?? 0;

        [NotMapped]
        public string BooksSummary
        {
            get
            {
                if (CHITIETDONHANGs == null || CHITIETDONHANGs.Count == 0)
                {
                    return "Chưa có chi tiết sách";
                }

                return string.Join(", ", CHITIETDONHANGs.Select(i => $"{i.TenSach} (x{i.SoLuongMua})"));
            }
        }

        [NotMapped]
        public string SelectedTransitionLabel
        {
            get => string.IsNullOrWhiteSpace(_selectedTransitionLabel) ? TrangThai : _selectedTransitionLabel;
            set => SetProperty(ref _selectedTransitionLabel, value);
        }

        [NotMapped]
        public string TrangThai => Status.ToVietnameseLabel();

        [NotMapped]
        public string PhuongThucThanhToan => Method.ToVietnameseLabel();

        [NotMapped]
        public string TrangThaiThanhToan => PaymentStatus.ToVietnameseLabel();

        [NotMapped]
        public string TrangThaiXuatKho => StockOutStatus.ToVietnameseLabel();

        [NotMapped]
        public string FormattedTongTien => $"{TongTien:N0} ₫";

        [NotMapped]
        public string FormattedNgayDat => _ngayDat.HasValue
            ? _ngayDat.Value.ToString("dd/MM/yyyy HH:mm")
            : NgayTao.ToString("dd/MM/yyyy");

        [NotMapped]
        public bool CanRecordPayment =>
            Status != OrderStatus.Cancelled &&
            PaymentStatus != PaymentStatus.Paid &&
            PaymentStatus != PaymentStatus.Refunded;

        [NotMapped]
        public bool IsAwaitingBankTransferConfirmation =>
            Method == PaymentMethod.BankTransfer &&
            PaymentStatus != PaymentStatus.Paid &&
            PaymentStatus != PaymentStatus.Refunded &&
            Status != OrderStatus.Cancelled;

        [NotMapped]
        public bool CanTransitionStatus => Status != OrderStatus.Completed || Status != OrderStatus.Cancelled;

        [NotMapped]
        public string RequiredActionText => Status switch
        {
            OrderStatus.New => IsAwaitingBankTransferConfirmation
                ? "Kiểm tra tiền về & Bấm 'Duyệt CK' để xác nhận đơn hàng"
                : "Xác nhận đơn hàng (Mới → Đã xác nhận)",
            OrderStatus.Confirmed => StockOutStatus == StockOutStatus.Issued
                ? "Chuyển trạng thái sang Đang giao"
                : "Thực hiện Xuất kho (UC10) trước khi giao hàng",
            OrderStatus.Shipping => PaymentStatus == PaymentStatus.Paid
                ? "Xác nhận Hoàn tất đơn hàng"
                : "Ghi nhận thu tiền (UC08) & Hoàn tất đơn",
            OrderStatus.Completed => "Đơn hàng đã hoàn tất",
            OrderStatus.Cancelled => PaymentStatus == PaymentStatus.Refunded
                ? "Đơn hàng đã hủy (Hoàn tiền cho khách)"
                : "Đơn hàng đã hủy",
            _ => "Kiểm tra đơn hàng"
        };
    }

    public partial class CHITIETDONHANG
    {
        private string? _tenSach;
        private string? _tacGia;
        private decimal? _donGia;

        [NotMapped]
        public string TenSach
        {
            get => !string.IsNullOrWhiteSpace(_tenSach)
                ? _tenSach
                : (MaSachNavigation?.Tensach ?? $"Sách #{MaSach}");
            set => _tenSach = value;
        }

        [NotMapped]
        public string TacGia
        {
            get => !string.IsNullOrWhiteSpace(_tacGia)
                ? _tacGia
                : (MaSachNavigation?.Tacgia ?? string.Empty);
            set => _tacGia = value;
        }

        [NotMapped]
        public int SoLuongMua
        {
            get => Soluong ?? 0;
            set
            {
                Soluong = (byte)Math.Clamp(value, 0, byte.MaxValue);
                if (_donGia.HasValue)
                {
                    Tong = _donGia.Value * Soluong.Value;
                }
            }
        }

        [NotMapped]
        public decimal DonGia
        {
            get
            {
                if (_donGia.HasValue)
                {
                    return _donGia.Value;
                }

                if (Soluong.HasValue && Soluong.Value > 0 && Tong.HasValue)
                {
                    return Math.Round(Tong.Value / Soluong.Value, 2);
                }

                return MaSachNavigation?.Giaban ?? 0m;
            }
            set
            {
                _donGia = value;
                if (Soluong.HasValue)
                {
                    Tong = value * Soluong.Value;
                }
            }
        }

        [NotMapped]
        public decimal ThanhTien => Tong ?? (DonGia * SoLuongMua);

        [NotMapped]
        public string FormattedDonGia => $"{DonGia:N0} ₫";

        [NotMapped]
        public string FormattedThanhTien => $"{ThanhTien:N0} ₫";
    }
}
