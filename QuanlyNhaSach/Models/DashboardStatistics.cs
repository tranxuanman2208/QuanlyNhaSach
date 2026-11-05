namespace QuanlyNhaSach.Models
{
    public class DashboardStatistics
    {
        public int NewOrdersCount { get; init; }
        public int ConfirmedOrdersCount { get; init; }
        public int ShippingOrdersCount { get; init; }
        public int CompletedOrdersCount { get; init; }
        public int CancelledOrdersCount { get; init; }
        public int UrgentOrdersCount { get; init; }
        public decimal CompletedRevenue { get; init; }
        public int LowStockBooksCount { get; init; }

        public string FormattedCompletedRevenue => $"{CompletedRevenue:N0} ₫";
    }
}
