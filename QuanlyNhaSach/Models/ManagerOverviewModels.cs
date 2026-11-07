using System;

namespace QuanlyNhaSach.Models
{
    public class RevenueChartPoint
    {
        public DateTime Date { get; init; }
        public string Label { get; init; } = string.Empty;
        public string FullDateLabel { get; init; } = string.Empty;
        public decimal Revenue { get; init; }
        public int OrderCount { get; init; }
        public double BarHeight { get; init; }
        public bool HasRevenue => Revenue > 0;

        /// <summary>Hiển thị nhãn giá trị phía trên cột (ẩn khi biểu đồ có quá nhiều cột).</summary>
        public bool ShowValueLabel { get; set; } = true;

        /// <summary>Hiển thị nhãn trục X (khi dày đặc chỉ hiện cách quãng để tránh chồng chữ).</summary>
        public bool ShowAxisLabel { get; set; } = true;

        public string TooltipText => $"{FullDateLabel}\nDoanh thu: {FormattedRevenue}\nSố đơn: {OrderCount}";
        public string FormattedRevenue => $"{Revenue:N0} ₫";
        public string ShortRevenueLabel => Revenue <= 0
            ? "0 ₫"
            : (Revenue >= 1_000_000m
                ? $"{Revenue / 1_000_000m:0.##} tr"
                : $"{Revenue / 1_000m:N0}k");
    }

    public class BestSellingBookItem
    {
        public int Rank { get; init; }
        public string MaSach { get; init; } = string.Empty;
        public string TenSach { get; init; } = string.Empty;
        public string TacGia { get; init; } = string.Empty;
        public int QuantitySold { get; init; }
        public decimal RevenueGenerated { get; init; }
        public string FormattedRevenue => $"{RevenueGenerated:N0} ₫";
        public string RankBadge => $"#{Rank}";
        public bool IsTopThree => Rank <= 3;
    }
}
