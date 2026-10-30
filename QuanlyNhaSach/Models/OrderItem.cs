namespace QuanlyNhaSach.Models
{
    public class OrderItem
    {
        public int MaSach { get; set; }
        public string TenSach { get; set; } = string.Empty;
        public string TacGia { get; set; } = string.Empty;
        public decimal DonGia { get; set; }
        public int SoLuongMua { get; set; }
        public decimal ThanhTien => DonGia * SoLuongMua;

        public string FormattedDonGia => $"{DonGia:N0} ₫";
        public string FormattedThanhTien => $"{ThanhTien:N0} ₫";
    }
}
