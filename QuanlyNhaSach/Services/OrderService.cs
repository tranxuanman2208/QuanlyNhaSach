using System;
using System.Collections.Generic;
using System.Linq;
using QuanlyNhaSach.Models;
using QuanlyNhaSach.Models.Enums;

namespace QuanlyNhaSach.Services
{
    public class OrderService
    {
        public const string AllFilterLabel = "Tất cả";

        private readonly MockDataService _mockDataService;

        public event EventHandler<Order?>? OrderUpdated;

        public OrderService(MockDataService mockDataService)
        {
            _mockDataService = mockDataService ?? throw new ArgumentNullException(nameof(mockDataService));
        }

        public IReadOnlyList<Order> GetAllOrders()
        {
            return _mockDataService.Orders
                .OrderByDescending(o => o.NgayDat)
                .ThenByDescending(o => o.MaDH)
                .ToList();
        }

        public Order? GetOrderById(int maDH)
        {
            return _mockDataService.GetOrderById(maDH);
        }

        public Order? FindOrderByCodeOrId(string? rawInput)
        {
            if (string.IsNullOrWhiteSpace(rawInput))
            {
                return null;
            }

            string cleaned = rawInput.Trim();
            if (cleaned.StartsWith("DH", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned.Substring(2).Trim();
            }
            cleaned = cleaned.TrimStart('#').Trim();

            if (int.TryParse(cleaned, out int parsedId))
            {
                return GetOrderById(parsedId);
            }

            return null;
        }

        public IReadOnlyList<Order> GetOrdersByStatus(string? statusFilterLabel)
        {
            var all = GetAllOrders();
            if (string.IsNullOrWhiteSpace(statusFilterLabel) ||
                statusFilterLabel.Equals(AllFilterLabel, StringComparison.OrdinalIgnoreCase))
            {
                return all;
            }

            if (EnumExtensions.TryParseOrderStatusLabel(statusFilterLabel, out var targetStatus))
            {
                return all.Where(o => o.Status == targetStatus).ToList();
            }

            return all;
        }

        public IReadOnlyList<Order> SearchOrders(string? keyword)
        {
            var all = GetAllOrders();
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return all;
            }

            string normalized = keyword.Trim();
            string cleanedId = normalized;
            if (cleanedId.StartsWith("DH", StringComparison.OrdinalIgnoreCase))
            {
                cleanedId = cleanedId.Substring(2).Trim();
            }
            cleanedId = cleanedId.TrimStart('#').Trim();

            return all.Where(o =>
                o.MaDH.ToString().Contains(cleanedId, StringComparison.OrdinalIgnoreCase) ||
                o.OrderCode.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                o.HoTenKhachHang.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                o.SDTKhachHang.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                o.TrangThai.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                (o.Items != null && o.Items.Any(i =>
                    i.TenSach.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                    i.MaSach.ToString().Equals(cleanedId, StringComparison.OrdinalIgnoreCase))))
                .ToList();
        }

        public IReadOnlyList<Order> GetUrgentOrders()
        {
            return _mockDataService.Orders
                .Where(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled)
                .OrderBy(o => o.Status switch
                {
                    OrderStatus.New => 0,
                    OrderStatus.Confirmed when o.StockOutStatus == StockOutStatus.InsufficientStock => 1,
                    OrderStatus.Confirmed => 2,
                    OrderStatus.Shipping => 3,
                    _ => 4
                })
                .ThenBy(o => o.NgayDat)
                .ToList();
        }

        public DashboardStatistics GetDashboardStatistics()
        {
            var orders = _mockDataService.Orders;
            var books = _mockDataService.Books;

            return new DashboardStatistics
            {
                NewOrdersCount = orders.Count(o => o.Status == OrderStatus.New),
                ConfirmedOrdersCount = orders.Count(o => o.Status == OrderStatus.Confirmed),
                ShippingOrdersCount = orders.Count(o => o.Status == OrderStatus.Shipping),
                CompletedOrdersCount = orders.Count(o => o.Status == OrderStatus.Completed),
                CancelledOrdersCount = orders.Count(o => o.Status == OrderStatus.Cancelled),
                UrgentOrdersCount = orders.Count(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled),
                CompletedRevenue = GetRealizedRevenue(),
                LowStockBooksCount = books.Count(b => b.IsActive && b.SoLuongTon < 10)
            };
        }

        /// <summary>
        /// Eligible completed &amp; paid orders for realized revenue and sales rankings (UC13).
        /// Excludes cancelled orders, pending/unpaid orders, and uncollected COD orders.
        /// </summary>
        public IReadOnlyList<Order> GetEligiblePaidCompletedOrders()
        {
            return _mockDataService.Orders
                .Where(o => o.Status == OrderStatus.Completed &&
                            o.PaymentStatus == PaymentStatus.Paid)
                .GroupBy(o => o.MaDH)
                .Select(g => g.First())
                .ToList();
        }

        public decimal GetRealizedRevenue()
        {
            return GetEligiblePaidCompletedOrders().Sum(o => o.TongTien);
        }

        public int GetTotalOrdersCount()
        {
            return _mockDataService.Orders.Count;
        }

        public IReadOnlyList<BestSellingBookItem> GetTopSellingBooks(int topCount = 5)
        {
            var eligibleOrders = GetEligiblePaidCompletedOrders();

            var aggregated = eligibleOrders
                .Where(o => o.Items != null)
                .SelectMany(o => o.Items)
                .GroupBy(i => i.MaSach)
                .Select(g =>
                {
                    var latestBook = _mockDataService.GetBookById(g.Key);
                    var firstItem = g.First();
                    return new
                    {
                        MaSach = g.Key,
                        TenSach = latestBook?.TenSach ?? firstItem.TenSach,
                        TacGia = latestBook?.TacGia ?? firstItem.TacGia,
                        QuantitySold = g.Sum(x => x.SoLuongMua),
                        RevenueGenerated = g.Sum(x => x.ThanhTien)
                    };
                })
                .Where(x => x.QuantitySold > 0)
                .OrderByDescending(x => x.QuantitySold)
                .ThenByDescending(x => x.RevenueGenerated)
                .ThenBy(x => x.MaSach)
                .Take(topCount)
                .ToList();

            var result = new List<BestSellingBookItem>(aggregated.Count);
            for (int idx = 0; idx < aggregated.Count; idx++)
            {
                var item = aggregated[idx];
                result.Add(new BestSellingBookItem
                {
                    Rank = idx + 1,
                    MaSach = item.MaSach,
                    TenSach = item.TenSach,
                    TacGia = item.TacGia,
                    QuantitySold = item.QuantitySold,
                    RevenueGenerated = item.RevenueGenerated
                });
            }

            return result;
        }

        public BestSellingBookItem? GetBestSellingBook()
        {
            return GetTopSellingBooks(1).FirstOrDefault();
        }

        public IReadOnlyList<RevenueChartPoint> GetRevenueChartPoints(int daysCount = 7, double maxBarPixelHeight = 175.0)
        {
            if (daysCount < 1)
            {
                daysCount = 7;
            }

            var eligibleOrders = GetEligiblePaidCompletedOrders();
            DateTime endDate = DateTime.Now.Date;
            if (eligibleOrders.Count > 0)
            {
                DateTime latestOrderDate = eligibleOrders.Max(o => o.NgayDat.Date);
                if (latestOrderDate > endDate)
                {
                    endDate = latestOrderDate;
                }
            }

            DateTime startDate = endDate.AddDays(-(daysCount - 1));

            var dailyTotals = new List<(DateTime Date, decimal Revenue, int Count)>(daysCount);
            for (int i = 0; i < daysCount; i++)
            {
                DateTime day = startDate.AddDays(i);
                var dayOrders = eligibleOrders.Where(o => o.NgayDat.Date == day).ToList();
                decimal dayRevenue = dayOrders.Sum(o => o.TongTien);
                dailyTotals.Add((day, dayRevenue, dayOrders.Count));
            }

            decimal maxRevenue = dailyTotals.Count > 0 ? dailyTotals.Max(x => x.Revenue) : 0m;

            var chartPoints = new List<RevenueChartPoint>(daysCount);
            foreach (var (date, revenue, count) in dailyTotals)
            {
                double barHeight;
                if (maxRevenue <= 0m || revenue <= 0m)
                {
                    barHeight = 6.0;
                }
                else
                {
                    double ratio = (double)(revenue / maxRevenue);
                    barHeight = Math.Max(18.0, Math.Round(ratio * maxBarPixelHeight, 1));
                }

                chartPoints.Add(new RevenueChartPoint
                {
                    Date = date,
                    Label = date.ToString("dd/MM"),
                    FullDateLabel = date.ToString("dd/MM/yyyy"),
                    Revenue = revenue,
                    OrderCount = count,
                    BarHeight = barHeight
                });
            }

            return chartPoints;
        }

        public IReadOnlyList<string> GetAllowedNextStatusLabels(Order? order)
        {
            if (order == null)
            {
                return Array.Empty<string>();
            }

            return order.Status switch
            {
                OrderStatus.New => new[]
                {
                    OrderStatus.Confirmed.ToVietnameseLabel(),
                    OrderStatus.Cancelled.ToVietnameseLabel()
                },
                OrderStatus.Confirmed => new[]
                {
                    OrderStatus.Shipping.ToVietnameseLabel(),
                    OrderStatus.Cancelled.ToVietnameseLabel()
                },
                OrderStatus.Shipping => new[]
                {
                    OrderStatus.Completed.ToVietnameseLabel()
                },
                _ => Array.Empty<string>()
            };
        }

        public OperationResult<Order> CreateCustomerOrder(
            string hoTen,
            string sdt,
            string diaChi,
            string phuongThucThanhToan,
            IEnumerable<CartItem> cartItems,
            int? authenticatedUserId = null)
        {
            var result = _mockDataService.CreateOrder(
                hoTen,
                sdt,
                diaChi,
                phuongThucThanhToan,
                cartItems,
                authenticatedUserId);

            if (result.IsSuccess && result.Data != null)
            {
                OrderUpdated?.Invoke(this, result.Data);
            }

            return result;
        }

        public OperationResult<Order> TransitionOrderStatusByLabel(int maDH, string? targetStatusLabel)
        {
            if (string.IsNullOrWhiteSpace(targetStatusLabel) ||
                !EnumExtensions.TryParseOrderStatusLabel(targetStatusLabel, out var targetStatus))
            {
                return OperationResult<Order>.Failure("Trạng thái đích được chọn không hợp lệ.");
            }

            return TransitionOrderStatus(maDH, targetStatus);
        }

        public OperationResult<Order> TransitionOrderStatus(int maDH, OrderStatus targetStatus)
        {
            var order = GetOrderById(maDH);
            if (order == null)
            {
                return OperationResult<Order>.Failure($"Không tìm thấy đơn hàng #{maDH} trong hệ thống.");
            }

            OrderStatus currentStatus = order.Status;

            if (currentStatus == targetStatus)
            {
                return OperationResult<Order>.Failure(
                    $"Đơn hàng #{order.MaDH} hiện đã ở trạng thái \"{currentStatus.ToVietnameseLabel()}\".");
            }

            // Rule 4: Cancelled order cannot re-enter active processing flow
            if (currentStatus == OrderStatus.Cancelled)
            {
                return OperationResult<Order>.Failure(
                    $"Đơn hàng #{order.MaDH} đã ở trạng thái \"Đã hủy\" và không thể khôi phục hay chuyển sang trạng thái khác.");
            }

            // Rule 5: Completed order cannot be reverted to an earlier status
            if (currentStatus == OrderStatus.Completed)
            {
                return OperationResult<Order>.Failure(
                    $"Đơn hàng #{order.MaDH} đã \"Hoàn tất\" và không được phép chuyển ngược về trạng thái trước đó.");
            }

            switch (currentStatus)
            {
                case OrderStatus.New:
                    if (targetStatus == OrderStatus.Confirmed)
                    {
                        order.Status = OrderStatus.Confirmed;
                        order.NgayCapNhat = DateTime.Now;
                        NotifyOrderUpdated(order);
                        return OperationResult<Order>.Success(
                            order,
                            $"Đã xác nhận đơn hàng #{order.MaDH} (Mới → Đã xác nhận).");
                    }

                    if (targetStatus == OrderStatus.Cancelled)
                    {
                        if (order.PaymentStatus == PaymentStatus.Paid || order.StockOutStatus == StockOutStatus.Issued)
                        {
                            return OperationResult<Order>.Failure(
                                $"Không thể hủy đơn hàng #{order.MaDH} vì đơn đã được thanh toán hoặc đã xuất kho.");
                        }

                        order.Status = OrderStatus.Cancelled;
                        order.NgayCapNhat = DateTime.Now;
                        NotifyOrderUpdated(order);
                        return OperationResult<Order>.Success(
                            order,
                            $"Đã hủy đơn hàng #{order.MaDH}.");
                    }

                    return OperationResult<Order>.Failure(
                        $"Chuyển trạng thái không hợp lệ: Đơn hàng #{order.MaDH} đang ở trạng thái \"Mới\" phải được chuyển sang \"Đã xác nhận\" trước khi giao hàng hoặc hoàn tất.");

                case OrderStatus.Confirmed:
                    if (targetStatus == OrderStatus.Shipping)
                    {
                        // Rule 10: Prevent advancing to Shipping until Stock-Out succeeds
                        if (order.StockOutStatus != StockOutStatus.Issued)
                        {
                            return OperationResult<Order>.Failure(
                                $"Không thể chuyển đơn hàng #{order.MaDH} sang \"Đang giao\" vì đơn hàng chưa hoàn tất xuất kho ({order.TrangThaiXuatKho}).\n\n" +
                                $"Vui lòng thực hiện nghiệp vụ Xuất kho (UC10) cho đơn hàng #{order.MaDH} trước khi giao hàng.");
                        }

                        order.Status = OrderStatus.Shipping;
                        order.NgayCapNhat = DateTime.Now;
                        NotifyOrderUpdated(order);
                        return OperationResult<Order>.Success(
                            order,
                            $"Đơn hàng #{order.MaDH} đã chuyển sang trạng thái \"Đang giao\" (Đã xác nhận → Đang giao).");
                    }

                    if (targetStatus == OrderStatus.Cancelled)
                    {
                        if (order.StockOutStatus == StockOutStatus.Issued)
                        {
                            return OperationResult<Order>.Failure(
                                $"Không thể hủy đơn hàng #{order.MaDH} vì sách của đơn hàng đã được xuất kho.");
                        }

                        if (order.PaymentStatus == PaymentStatus.Paid)
                        {
                            return OperationResult<Order>.Failure(
                                $"Không thể hủy đơn hàng #{order.MaDH} vì khách hàng đã thanh toán.");
                        }

                        order.Status = OrderStatus.Cancelled;
                        order.NgayCapNhat = DateTime.Now;
                        NotifyOrderUpdated(order);
                        return OperationResult<Order>.Success(
                            order,
                            $"Đã hủy đơn hàng #{order.MaDH}.");
                    }

                    return OperationResult<Order>.Failure(
                        $"Chuyển trạng thái không hợp lệ từ \"Đã xác nhận\" sang \"{targetStatus.ToVietnameseLabel()}\". Luồng hợp lệ tiếp theo là \"Đang giao\".");

                case OrderStatus.Shipping:
                    if (targetStatus == OrderStatus.Completed)
                    {
                        if (order.StockOutStatus != StockOutStatus.Issued)
                        {
                            return OperationResult<Order>.Failure(
                                $"Đơn hàng #{order.MaDH} chưa có xác nhận xuất kho hợp lệ nên không thể Hoàn tất.");
                        }

                        if (order.PaymentStatus != PaymentStatus.Paid)
                        {
                            return OperationResult<Order>.Failure(
                                $"Không thể chuyển đơn hàng #{order.MaDH} sang \"Hoàn tất\" vì thanh toán chưa hoàn tất (Trạng thái thanh toán: {order.TrangThaiThanhToan}).\n\n" +
                                $"Vui lòng vào mục \"Bán hàng\" (UC08) để ghi nhận thu tiền trước khi hoàn tất đơn hàng.");
                        }

                        order.Status = OrderStatus.Completed;
                        order.NgayCapNhat = DateTime.Now;
                        NotifyOrderUpdated(order);
                        return OperationResult<Order>.Success(
                            order,
                            $"Đơn hàng #{order.MaDH} đã được cập nhật sang trạng thái \"Hoàn tất\" (Đang giao → Hoàn tất).");
                    }

                    return OperationResult<Order>.Failure(
                        $"Đơn hàng #{order.MaDH} đang ở trạng thái \"Đang giao\" chỉ có thể chuyển tiếp sang \"Hoàn tất\" khi đủ điều kiện.");

                default:
                    return OperationResult<Order>.Failure("Trạng thái đơn hàng không hỗ trợ chuyển đổi.");
            }
        }

        public OperationResult<Order> RecordPayment(
            int maDH,
            PaymentMethod selectedMethod,
            decimal? enteredAmount,
            bool confirmCodCollectionOnDelivery = false)
        {
            var order = GetOrderById(maDH);
            if (order == null)
            {
                return OperationResult<Order>.Failure($"Không tìm thấy đơn hàng #{maDH} trong hệ thống.");
            }

            if (order.Status == OrderStatus.Cancelled)
            {
                return OperationResult<Order>.Failure(
                    $"Đơn hàng #{order.MaDH} đã hủy. Không thể ghi nhận thanh toán cho đơn hàng đã hủy.");
            }

            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                return OperationResult<Order>.Failure(
                    $"Đơn hàng #{order.MaDH} đã được ghi nhận thanh toán trước đó ({order.FormattedPaidAmount}). Hệ thống từ chối ghi nhận thanh toán trùng lặp.");
            }

            switch (selectedMethod)
            {
                case PaymentMethod.Cash:
                {
                    if (!enteredAmount.HasValue || enteredAmount.Value <= 0)
                    {
                        return OperationResult<Order>.Failure("Vui lòng nhập số tiền mặt khách đưa hợp lệ (lớn hơn 0).");
                    }

                    if (enteredAmount.Value < order.TongTien)
                    {
                        decimal shortage = order.TongTien - enteredAmount.Value;
                        return OperationResult<Order>.Failure(
                            $"Số tiền mặt khách đưa ({enteredAmount.Value:N0} ₫) nhỏ hơn tổng tiền đơn hàng ({order.FormattedTongTien}). Còn thiếu: {shortage:N0} ₫.");
                    }

                    decimal change = enteredAmount.Value - order.TongTien;

                    order.Method = PaymentMethod.Cash;
                    order.PaymentStatus = PaymentStatus.Paid;
                    order.PaidAmount = enteredAmount.Value;
                    order.PaymentNote = change > 0
                        ? $"Thu tiền mặt: {enteredAmount.Value:N0} ₫ (Tiền thừa trả khách: {change:N0} ₫)"
                        : $"Thu đủ tiền mặt: {enteredAmount.Value:N0} ₫";
                    order.NgayCapNhat = DateTime.Now;

                    if (order.Status == OrderStatus.New)
                    {
                        order.Status = OrderStatus.Confirmed;
                    }
                    else if (order.Status == OrderStatus.Shipping && order.StockOutStatus == StockOutStatus.Issued)
                    {
                        order.Status = OrderStatus.Completed;
                    }

                    NotifyOrderUpdated(order);

                    string completionHint = order.Status == OrderStatus.Completed
                        ? "Đơn hàng đã đủ điều kiện xuất kho & giao hàng nên được chuyển sang trạng thái 'Hoàn tất'."
                        : $"Trạng thái đơn hàng hiện tại: '{order.TrangThai}' (Cần hoàn tất Xuất kho & Giao hàng để đóng đơn).";

                    return OperationResult<Order>.Success(
                        order,
                        $"Đã ghi nhận thanh toán TIỀN MẶT cho đơn hàng #{order.MaDH}!\n" +
                        $"• Số tiền khách đưa: {enteredAmount.Value:N0} ₫\n" +
                        $"• Tổng tiền đơn: {order.FormattedTongTien}\n" +
                        $"• Tiền thừa trả khách: {change:N0} ₫\n" +
                        $"• Trạng thái thanh toán: {order.TrangThaiThanhToan}\n" +
                        $"• {completionHint}");
                }

                case PaymentMethod.BankTransfer:
                {
                    if (!enteredAmount.HasValue || enteredAmount.Value <= 0)
                    {
                        return OperationResult<Order>.Failure("Vui lòng nhập số tiền chuyển khoản cần xác nhận.");
                    }

                    if (enteredAmount.Value < order.TongTien)
                    {
                        decimal shortage = order.TongTien - enteredAmount.Value;
                        return OperationResult<Order>.Failure(
                            $"Số tiền chuyển khoản xác nhận ({enteredAmount.Value:N0} ₫) nhỏ hơn số tiền cần thanh toán ({order.FormattedTongTien}). Còn thiếu: {shortage:N0} ₫.");
                    }

                    order.Method = PaymentMethod.BankTransfer;
                    order.PaymentStatus = PaymentStatus.Paid;
                    order.PaidAmount = enteredAmount.Value;
                    order.PaymentNote = $"[Mô phỏng nội bộ]: Nhân viên xác nhận chuyển khoản {enteredAmount.Value:N0} ₫ (Không kết nối ngân hàng thực tế)";
                    order.NgayCapNhat = DateTime.Now;

                    if (order.Status == OrderStatus.New)
                    {
                        order.Status = OrderStatus.Confirmed;
                    }
                    else if (order.Status == OrderStatus.Shipping && order.StockOutStatus == StockOutStatus.Issued)
                    {
                        order.Status = OrderStatus.Completed;
                    }

                    NotifyOrderUpdated(order);

                    string completionHint = order.Status == OrderStatus.Completed
                        ? "Đơn hàng đã đủ điều kiện xuất kho & giao hàng nên được chuyển sang trạng thái 'Hoàn tất'."
                        : $"Trạng thái đơn hàng hiện tại: '{order.TrangThai}' (Cần hoàn tất Xuất kho & Giao hàng để đóng đơn).";

                    return OperationResult<Order>.Success(
                        order,
                        $"Đã ghi nhận xác nhận CHUYỂN KHOẢN (Mô phỏng) cho đơn hàng #{order.MaDH}!\n" +
                        $"• Số tiền ghi nhận: {enteredAmount.Value:N0} ₫\n" +
                        $"• Trạng thái thanh toán: {order.TrangThaiThanhToan}\n" +
                        $"• Lưu ý: Đây là xác nhận nghiệp vụ mô phỏng của nhân viên trên dữ liệu In-Memory, không kiểm chứng giao dịch ngân hàng thực tế.\n" +
                        $"• {completionHint}");
                }

                case PaymentMethod.COD:
                {
                    // Case 1: Staff is explicitly confirming COD cash collection upon delivery
                    if (confirmCodCollectionOnDelivery)
                    {
                        if (order.StockOutStatus != StockOutStatus.Issued)
                        {
                            return OperationResult<Order>.Failure(
                                $"Không thể xác nhận đã thu tiền COD khi giao hàng vì đơn hàng #{order.MaDH} chưa được xuất kho ({order.TrangThaiXuatKho}).");
                        }

                        if (order.Status != OrderStatus.Shipping)
                        {
                            return OperationResult<Order>.Failure(
                                $"Chỉ có thể xác nhận thu tiền COD khi đơn hàng đang ở trạng thái \"Đang giao\" (Trạng thái hiện tại: \"{order.TrangThai}\").");
                        }

                        if (!enteredAmount.HasValue || enteredAmount.Value < order.TongTien)
                        {
                            return OperationResult<Order>.Failure(
                                $"Số tiền thu hộ COD ({enteredAmount ?? 0:N0} ₫) không được nhỏ hơn tổng tiền đơn hàng ({order.FormattedTongTien}).");
                        }

                        order.Method = PaymentMethod.COD;
                        order.PaymentStatus = PaymentStatus.Paid;
                        order.PaidAmount = enteredAmount.Value;
                        order.PaymentNote = $"[Mô phỏng]: Đã thu tiền hộ COD ({enteredAmount.Value:N0} ₫) khi giao hàng thành công";
                        order.Status = OrderStatus.Completed;
                        order.NgayCapNhat = DateTime.Now;

                        NotifyOrderUpdated(order);

                        return OperationResult<Order>.Success(
                            order,
                            $"Đã xác nhận thu hộ COD và HOÀN TẤT đơn hàng #{order.MaDH}!\n" +
                            $"• Số tiền thu hộ: {enteredAmount.Value:N0} ₫\n" +
                            $"• Trạng thái thanh toán: {order.TrangThaiThanhToan}\n" +
                            $"• Trạng thái đơn hàng: {order.TrangThai}");
                    }

                    // Case 2: Standard COD arrangement — record that payment is expected on delivery (do NOT mark Paid or Completed)
                    order.Method = PaymentMethod.COD;
                    order.PaymentStatus = PaymentStatus.PayOnDelivery;
                    order.PaidAmount = 0m;
                    order.PaymentNote = "Thanh toán thu hộ (COD) khi khách nhận hàng — chưa thu tiền trước";
                    order.NgayCapNhat = DateTime.Now;

                    if (order.Status == OrderStatus.New)
                    {
                        order.Status = OrderStatus.Confirmed;
                    }

                    NotifyOrderUpdated(order);

                    return OperationResult<Order>.Success(
                        order,
                        $"Đã cập nhật phương thức thanh toán COD (Thu hộ khi giao hàng) cho đơn hàng #{order.MaDH}.\n" +
                        $"• Trạng thái thanh toán: {order.TrangThaiThanhToan} (Chưa thu tiền)\n" +
                        $"• Trạng thái đơn hàng: {order.TrangThai}\n" +
                        $"• Lưu ý: Đơn hàng COD không được tính là 'Đã thanh toán' hay 'Hoàn tất' cho đến khi sách được xuất kho, giao tới khách hàng và xác nhận thu tiền thực tế.");
                }

                default:
                    return OperationResult<Order>.Failure("Phương thức thanh toán không được hỗ trợ.");
            }
        }

        private void NotifyOrderUpdated(Order order)
        {
            OrderUpdated?.Invoke(this, order);
            _mockDataService.NotifyDataChanged();
        }
    }
}
