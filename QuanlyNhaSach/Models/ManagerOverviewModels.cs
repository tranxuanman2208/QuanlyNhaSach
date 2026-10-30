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
        public int MaSach { get; init; }
        public string TenSach { get; init; } = string.Empty;
        public string TacGia { get; init; } = string.Empty;
        public int QuantitySold { get; init; }
        public decimal RevenueGenerated { get; init; }
        public string FormattedRevenue => $"{RevenueGenerated:N0} ₫";
        public string RankBadge => $"#{Rank}";
        public bool IsTopThree => Rank <= 3;
    }
}
