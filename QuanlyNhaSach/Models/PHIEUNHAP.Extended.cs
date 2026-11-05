using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using QuanlyNhaSach.Models.Enums;

namespace QuanlyNhaSach.Models
{
    public partial class PHIEUNHAP
    {
        private string? _tenNCC;
        private string? _sdtNCC;
        private string? _emailNCC;
        private string? _diaChiNCC;
        private DateTime? _ngayLap;
        private string? _nguoiLapHoTen;

        [NotMapped]
        public string ReceiptCode => $"PN{MaPN:D4}";

        [NotMapped]
        public string TenNCC
        {
            get => !string.IsNullOrWhiteSpace(_tenNCC)
                ? _tenNCC
                : (MaNCCNavigation?.TenNCC ?? (string.IsNullOrWhiteSpace(MaNCC) ? "N/A" : MaNCC));
            set => _tenNCC = value;
        }

        [NotMapped]
        public string SdtNCC
        {
            get => !string.IsNullOrWhiteSpace(_sdtNCC)
                ? _sdtNCC
                : (MaNCCNavigation?.SDT ?? "N/A");
            set => _sdtNCC = value;
        }

        [NotMapped]
        public string EmailNCC
        {
            get => !string.IsNullOrWhiteSpace(_emailNCC)
                ? _emailNCC
                : (MaNCCNavigation?.Email ?? "N/A");
            set => _emailNCC = value;
        }

        [NotMapped]
        public string DiaChiNCC
        {
            get => !string.IsNullOrWhiteSpace(_diaChiNCC)
                ? _diaChiNCC
                : (MaNCCNavigation?.Diachi ?? "N/A");
            set => _diaChiNCC = value;
        }

        [NotMapped]
        public DateTime NgayLap
        {
            get => _ngayLap ?? NgayNhap.ToDateTime(TimeOnly.MinValue);
            set
            {
                _ngayLap = value;
                NgayNhap = DateOnly.FromDateTime(value);
            }
        }

        [NotMapped]
        public decimal TongTien
        {
            get => Tongtien ?? (CHITIETPHIEUNHAPs?.Sum(d => d.ThanhTien) ?? 0m);
            set => Tongtien = value;
        }

        [NotMapped]
        public ImportReceiptStatus TrangThai
        {
            get => (Dathanhtoan ?? true) ? ImportReceiptStatus.Confirmed : ImportReceiptStatus.Draft;
            set => Dathanhtoan = (value == ImportReceiptStatus.Confirmed);
        }

        [NotMapped]
        public string NguoiLapHoTen
        {
            get => !string.IsNullOrWhiteSpace(_nguoiLapHoTen)
                ? _nguoiLapHoTen
                : (MaQlyNavigation?.Hoten ?? (string.IsNullOrWhiteSpace(MaQly) ? "Quản lý kho" : MaQly));
            set => _nguoiLapHoTen = value;
        }

        [NotMapped]
        public string GhiChu { get; set; } = "Nhập bổ sung sách vào kho";

        [NotMapped]
        public List<CHITIETPHIEUNHAP> Details => CHITIETPHIEUNHAPs?.ToList() ?? new List<CHITIETPHIEUNHAP>();

        [NotMapped]
        public int DistinctBookCount => CHITIETPHIEUNHAPs?.Count ?? 0;

        [NotMapped]
        public int TotalImportedQuantity => CHITIETPHIEUNHAPs?.Sum(d => d.SoLuongNhap) ?? 0;

        [NotMapped]
        public string BooksSummary
        {
            get
            {
                if (CHITIETPHIEUNHAPs == null || CHITIETPHIEUNHAPs.Count == 0)
                {
                    return "0 đầu sách";
                }

                var titles = CHITIETPHIEUNHAPs
                    .Select(d => string.IsNullOrWhiteSpace(d.TenSach) ? d.MaSach : d.TenSach)
                    .Take(2)
                    .ToList();
                string joined = string.Join(", ", titles);
                return CHITIETPHIEUNHAPs.Count > 2
                    ? $"{CHITIETPHIEUNHAPs.Count} đầu sách ({joined}, ...)"
                    : $"{CHITIETPHIEUNHAPs.Count} đầu sách ({joined})";
            }
        }

        [NotMapped]
        public string FormattedTongTien => $"{TongTien:N0} ₫";

        [NotMapped]
        public string FormattedNgayLap => _ngayLap.HasValue
            ? _ngayLap.Value.ToString("dd/MM/yyyy HH:mm")
            : NgayNhap.ToString("dd/MM/yyyy");

        [NotMapped]
        public string TrangThaiLabel => TrangThai.ToVietnameseLabel();
    }

    public partial class CHITIETPHIEUNHAP : ObservableObject
    {
        public event EventHandler? LineValueChanged;

        private string? _tenSach;
        private string? _tacGia;
        private int? _currentStock;
        private int? _soLuongNhapOverride;
        private decimal? _donGiaNhapOverride;

        [NotMapped]
        public string BookCode => string.IsNullOrWhiteSpace(MaSach) ? string.Empty : MaSach.Trim();

        [NotMapped]
        public string TenSach
        {
            get => !string.IsNullOrWhiteSpace(_tenSach)
                ? _tenSach
                : (MaSachNavigation?.Tensach ?? $"Sách #{MaSach}");
            set => SetProperty(ref _tenSach, value);
        }

        [NotMapped]
        public string TacGia
        {
            get => !string.IsNullOrWhiteSpace(_tacGia)
                ? _tacGia
                : (MaSachNavigation?.Tacgia ?? string.Empty);
            set => SetProperty(ref _tacGia, value);
        }

        [NotMapped]
        public int CurrentStock
        {
            get => _currentStock ?? (MaSachNavigation?.Soluongton ?? 0);
            set
            {
                if (_currentStock != value)
                {
                    _currentStock = value;
                    OnPropertyChanged();
                }
            }
        }

        [NotMapped]
        public int SoLuongNhap
        {
            get => _soLuongNhapOverride ?? (Soluongnhap ?? 1);
            set
            {
                if (SoLuongNhap != value)
                {
                    _soLuongNhapOverride = value;
                    if (value >= 0 && value <= byte.MaxValue)
                    {
                        Soluongnhap = (byte)value;
                    }

                    Tong = Math.Max(0, value) * Math.Max(0m, DonGiaNhap);
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Soluongnhap));
                    OnPropertyChanged(nameof(Tong));
                    OnPropertyChanged(nameof(ThanhTien));
                    OnPropertyChanged(nameof(FormattedThanhTien));
                    LineValueChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        [NotMapped]
        public decimal DonGiaNhap
        {
            get
            {
                if (_donGiaNhapOverride.HasValue)
                {
                    return _donGiaNhapOverride.Value;
                }

                if (Soluongnhap.HasValue && Soluongnhap.Value > 0 && Tong.HasValue)
                {
                    return Math.Round(Tong.Value / Soluongnhap.Value, 2);
                }

                return MaSachNavigation?.Giaban ?? 0m;
            }
            set
            {
                if (DonGiaNhap != value)
                {
                    _donGiaNhapOverride = value;
                    Tong = Math.Max(0, SoLuongNhap) * Math.Max(0m, value);
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Tong));
                    OnPropertyChanged(nameof(ThanhTien));
                    OnPropertyChanged(nameof(FormattedDonGiaNhap));
                    OnPropertyChanged(nameof(FormattedThanhTien));
                    LineValueChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        [NotMapped]
        public decimal ThanhTien => _soLuongNhapOverride.HasValue || _donGiaNhapOverride.HasValue
            ? Math.Max(0, SoLuongNhap) * Math.Max(0m, DonGiaNhap)
            : (Tong ?? (Math.Max(0, SoLuongNhap) * Math.Max(0m, DonGiaNhap)));

        [NotMapped]
        public string FormattedDonGiaNhap => $"{DonGiaNhap:N0} ₫";

        [NotMapped]
        public string FormattedThanhTien => $"{ThanhTien:N0} ₫";
    }
}
