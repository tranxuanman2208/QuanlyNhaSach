using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using QuanlyNhaSach.Models.Enums;

namespace QuanlyNhaSach.Models
{
    public partial class ImportReceipt : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ReceiptCode))]
        private int _maPN;

        [ObservableProperty]
        private int _maNCC;

        [ObservableProperty]
        private string _tenNCC = string.Empty;

        [ObservableProperty]
        private string _sdtNCC = string.Empty;

        [ObservableProperty]
        private string _emailNCC = string.Empty;

        [ObservableProperty]
        private string _diaChiNCC = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FormattedNgayLap))]
        private DateTime _ngayLap = DateTime.Now;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FormattedTongTien))]
        private decimal _tongTien;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TrangThaiLabel))]
        private ImportReceiptStatus _trangThai = ImportReceiptStatus.Confirmed;

        [ObservableProperty]
        private int _nguoiLapMaND;

        [ObservableProperty]
        private string _nguoiLapHoTen = string.Empty;

        [ObservableProperty]
        private string _ghiChu = string.Empty;

        public ObservableCollection<ImportReceiptDetail> Details { get; init; } = new();

        public string ReceiptCode => $"PN{MaPN:D4}";
        public string TrangThaiLabel => TrangThai.ToVietnameseLabel();
        public int DistinctBookCount => Details?.Count ?? 0;
        public int TotalImportedQuantity => Details?.Sum(d => d.SoLuongNhap) ?? 0;
        public string FormattedTongTien => $"{TongTien:N0} ₫";
        public string FormattedNgayLap => NgayLap.ToString("dd/MM/yyyy HH:mm");
        public string BooksSummary => Details == null || Details.Count == 0
            ? "Không có chi tiết"
            : string.Join(", ", Details.Select(d => $"{d.TenSach} (x{d.SoLuongNhap})"));
    }
}
