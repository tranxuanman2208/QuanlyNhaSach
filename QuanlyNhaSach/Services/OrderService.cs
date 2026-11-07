using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using QuanlyNhaSach.Models;
using QuanlyNhaSach.Models.Enums;

namespace QuanlyNhaSach.Services
{
    public class OrderService
    {
        public const string AllFilterLabel = "Tất cả";
        private const decimal MaxSqlDecimal10_2 = 99_999_999.99m;

        private readonly object _cacheLock = new();
        private List<DONHANG>? _ordersCache;

        public event EventHandler<DONHANG?>? OrderUpdated;

        public OrderService()
        {
        }

        public void InvalidateCache()
        {
            lock (_cacheLock)
            {
                _ordersCache = null;
            }
        }

        public IReadOnlyList<DONHANG> GetAllOrders()
        {
            lock (_cacheLock)
            {
                if (_ordersCache != null)
                {
                    return _ordersCache;
                }

                try
                {
                    using var db = new QuanlyNhaSachContext();
                    _ordersCache = db.DONHANGs
                        .AsNoTracking()
                        .Include(o => o.MaKHNavigation)
                        .Include(o => o.CHITIETDONHANGs)
                            .ThenInclude(i => i.MaSachNavigation)
                        .Include(o => o.PHIEUXUATs)
                        .OrderByDescending(o => o.NgayTao)
                        .ThenByDescending(o => o.MaDH)
                        .ToList();

                    return _ordersCache;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Lỗi khi tải danh sách đơn hàng: {ex.Message}");
                    return Array.Empty<DONHANG>();
                }
            }
        }

        public DONHANG? GetOrderById(int maDH)
        {
            var cached = GetAllOrders().FirstOrDefault(o => o.MaDH == maDH);
            if (cached != null)
            {
                return cached;
            }

            return GetFreshOrderFromDatabase(maDH);
        }

        public DONHANG? GetFreshOrderFromDatabase(int maDH)
        {
            try
            {
                using var db = new QuanlyNhaSachContext();
                return db.DONHANGs
                    .AsNoTracking()
                    .Include(o => o.MaKHNavigation)
                    .Include(o => o.CHITIETDONHANGs)
                        .ThenInclude(i => i.MaSachNavigation)
                    .Include(o => o.PHIEUXUATs)
                    .FirstOrDefault(o => o.MaDH == maDH);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi tìm đơn hàng #{maDH}: {ex.Message}");
                return null;
            }
        }

        public DONHANG? FindOrderByCodeOrId(string? rawInput)
        {
            if (string.IsNullOrWhiteSpace(rawInput))
            {
                return null;
            }

            string cleaned = rawInput.Trim().TrimStart('#').Trim();
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

        public IReadOnlyList<DONHANG> GetOrdersByStatus(string? statusFilterLabel)
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

        public IReadOnlyList<DONHANG> SearchOrders(string? keyword)
        {
            var all = GetAllOrders();
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return all;
            }

            string normalized = keyword.Trim();
            string cleanedId = normalized.TrimStart('#').Trim();
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
                (o.CHITIETDONHANGs != null && o.CHITIETDONHANGs.Any(i =>
                    i.TenSach.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                    i.MaSach.Trim().Equals(cleanedId, StringComparison.OrdinalIgnoreCase))))
                .ToList();
        }

        public IReadOnlyList<DONHANG> GetUrgentOrders()
        {
            return GetAllOrders()
                .Where(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled)
                .ToList();
        }

        public DashboardStatistics GetDashboardStatistics()
        {
            var orders = GetAllOrders();
            int lowStockBooksCount = 0;

            try
            {
                using var db = new QuanlyNhaSachContext();
                lowStockBooksCount = db.SACHs
                    .AsNoTracking()
                    .Count(b => (b.Daxoa == null || b.Daxoa == false) && (b.Soluongton ?? 0) <= 5);
            }
            catch
            {
                lowStockBooksCount = 0;
            }

            int newCount = orders.Count(o => o.Status == OrderStatus.New);
            int confirmedCount = orders.Count(o => o.Status == OrderStatus.Confirmed);
            int shippingCount = orders.Count(o => o.Status == OrderStatus.Shipping);
            int completedCount = orders.Count(o => o.Status == OrderStatus.Completed);
            int cancelledCount = orders.Count(o => o.Status == OrderStatus.Cancelled);

            decimal completedRevenue = orders
                .Where(o => o.Status == OrderStatus.Completed && o.PaymentStatus == PaymentStatus.Paid)
                .Sum(o => o.TongTien);

            return new DashboardStatistics
            {
                NewOrdersCount = newCount,
                ConfirmedOrdersCount = confirmedCount,
                ShippingOrdersCount = shippingCount,
                CompletedOrdersCount = completedCount,
                CancelledOrdersCount = cancelledCount,
                UrgentOrdersCount = newCount + confirmedCount + shippingCount,
                CompletedRevenue = completedRevenue,
                LowStockBooksCount = lowStockBooksCount
            };
        }

        public IReadOnlyList<DONHANG> GetEligiblePaidCompletedOrders()
        {
            return GetAllOrders()
                .Where(o => o.Status == OrderStatus.Completed && o.PaymentStatus == PaymentStatus.Paid)
                .GroupBy(o => o.MaDH)
                .Select(g => g.First())
                .ToList();
        }

        public int GetTotalOrdersCount()
        {
            return GetAllOrders().Count;
        }

        public (int CompletedCount, int ProcessingCount, int CancelledCount) GetOrderStatusBreakdown()
        {
            var orders = GetAllOrders();
            int completed = orders.Count(o => o.Status == OrderStatus.Completed);
            int cancelled = orders.Count(o => o.Status == OrderStatus.Cancelled);
            int processing = Math.Max(0, orders.Count - completed - cancelled);
            return (completed, processing, cancelled);
        }

        public IReadOnlyList<BestSellingBookItem> GetTopSellingBooks(int topCount = 5)
        {
            if (topCount < 1)
            {
                topCount = 5;
            }

            var eligibleOrders = GetEligiblePaidCompletedOrders();
            var lineItems = eligibleOrders
                .Where(o => o.CHITIETDONHANGs != null)
                .SelectMany(o => o.CHITIETDONHANGs)
                .ToList();

            if (lineItems.Count == 0)
            {
                return Array.Empty<BestSellingBookItem>();
            }

            var grouped = lineItems
                .GroupBy(i => i.MaSach?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var sample = g.First();
                    return new
                    {
                        MaSach = g.Key,
                        TenSach = sample.TenSach,
                        TacGia = sample.TacGia,
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

            var result = new List<BestSellingBookItem>(grouped.Count);
            for (int idx = 0; idx < grouped.Count; idx++)
            {
                var item = grouped[idx];
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
                    FullDateLabel = $"Ngày {date:dd/MM/yyyy}",
                    Revenue = revenue,
                    OrderCount = count,
                    BarHeight = barHeight
                });
            }

            return chartPoints;
        }

        public IReadOnlyList<RevenueChartPoint> GetMonthlyRevenueChartPoints(int monthsCount = 12, double maxBarPixelHeight = 175.0)
        {
            if (monthsCount < 1)
            {
                monthsCount = 12;
            }

            var eligibleOrders = GetEligiblePaidCompletedOrders();
            DateTime now = DateTime.Now;

            var monthlyTotals = new List<(DateTime MonthDate, decimal Revenue, int Count)>(monthsCount);
            for (int i = monthsCount - 1; i >= 0; i--)
            {
                DateTime monthDate = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                var monthOrders = eligibleOrders
                    .Where(o => o.NgayDat.Year == monthDate.Year && o.NgayDat.Month == monthDate.Month)
                    .ToList();
                decimal monthRevenue = monthOrders.Sum(o => o.TongTien);
                monthlyTotals.Add((monthDate, monthRevenue, monthOrders.Count));
            }

            decimal maxRevenue = monthlyTotals.Count > 0 ? monthlyTotals.Max(x => x.Revenue) : 0m;

            var chartPoints = new List<RevenueChartPoint>(monthsCount);
            foreach (var (monthDate, revenue, count) in monthlyTotals)
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
                    Date = monthDate,
                    Label = $"T{monthDate.Month:D2}/{monthDate:yy}",
                    FullDateLabel = $"Tháng {monthDate.Month:D2}/{monthDate.Year}",
                    Revenue = revenue,
                    OrderCount = count,
                    BarHeight = barHeight
                });
            }

            return chartPoints;
        }

        public IReadOnlyList<string> GetAllowedNextStatusLabels(DONHANG? order)
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

        public OperationResult<DONHANG> CreateCustomerOrder(
            string hoTen,
            string sdt,
            string email,
            string diaChi,
            string phuongThucThanhToan,
            IEnumerable<CartItem> cartItems,
            string? authenticatedCustomerMaND = null)
        {
            if (string.IsNullOrWhiteSpace(hoTen))
            {
                return OperationResult<DONHANG>.Failure("Vui lòng nhập họ và tên người nhận.");
            }

            if (string.IsNullOrWhiteSpace(sdt) || !Regex.IsMatch(sdt.Trim(), @"^\d{9,10}$"))
            {
                return OperationResult<DONHANG>.Failure("Số điện thoại người nhận không hợp lệ (gồm 9–10 chữ số).");
            }
            if (string.IsNullOrWhiteSpace(email) || !email.EndsWith("@gmail.com") || email.Length < 10)
                return OperationResult<DONHANG>.Failure("Vui lòng nhập email người đặt hàng");
            if (string.IsNullOrWhiteSpace(diaChi))
            {
                return OperationResult<DONHANG>.Failure("Vui lòng nhập địa chỉ giao hàng.");
            }

            var itemsList = cartItems?.ToList() ?? new List<CartItem>();
            if (itemsList.Count == 0)
            {
                return OperationResult<DONHANG>.Failure("Giỏ hàng đang trống. Vui lòng chọn ít nhất 1 cuốn sách.");
            }

            if (itemsList.Any(c => c.Book == null || string.IsNullOrWhiteSpace(c.Book.MaSach)))
            {
                return OperationResult<DONHANG>.Failure("Sản phẩm không hợp lệ.");
            }

            string normalizedName = hoTen.Trim();
            string normalizedPhone = sdt.Trim();
            string normalizedAddress = diaChi.Trim();
            string normalizedEmail = email.Trim();
            PaymentMethod method = EnumExtensions.ParsePaymentMethod(phuongThucThanhToan);

            try
            {
                using var db = new QuanlyNhaSachContext();
                using var transaction = db.Database.BeginTransaction();

                try
                {
                    NGUOIDUNG? customer = null;
                    if (!string.IsNullOrWhiteSpace(authenticatedCustomerMaND))
                    {
                        string key = authenticatedCustomerMaND.Trim();
                        customer = db.NGUOIDUNGs.FirstOrDefault(u => u.MaND == key);
                    }

                    customer ??= db.NGUOIDUNGs.FirstOrDefault(u => u.SDT == normalizedPhone);

                    if (customer == null)
                    {
                        string newCustomerId = GenerateNextCustomerId(db);
                        string loginKey = normalizedPhone.Length <= 10
                            ? normalizedPhone
                            : normalizedPhone.Substring(0, 10);

                        if (db.NGUOIDUNGs.AsNoTracking().Any(u => u.Tendangnhap == loginKey))
                        {
                            loginKey = newCustomerId;
                        }

                        customer = new NGUOIDUNG
                        {
                            MaND = newCustomerId,
                            Tendangnhap = loginKey,
                            Matkhau = BCrypt.Net.BCrypt.HashPassword("123456"),
                            Hoten = normalizedName,
                            SDT = normalizedPhone,
                            Email = normalizedEmail,
                            Diachi = normalizedAddress,
                            Vaitro = UserRole.Customer.ToVietnameseLabel(),
                            Trangthai = AccountStatus.Active.ToVietnameseLabel()
                        };
                        db.NGUOIDUNGs.Add(customer);
                        db.SaveChanges();
                    }
                    else
                    {
                        customer.Hoten = normalizedName;
                        customer.SDT = normalizedPhone;
                        customer.Diachi = normalizedAddress;
                    }

                    var bookKeys = itemsList.Select(c => c.Book.MaSach.Trim()).Distinct().ToList();
                    var dbBooksMap = db.SACHs
                        .AsNoTracking()
                        .Where(b => bookKeys.Contains(b.MaSach) && (b.Daxoa == null || b.Daxoa == false))
                        .ToDictionary(b => b.MaSach.Trim(), b => b, StringComparer.OrdinalIgnoreCase);

                    decimal grandTotal = 0m;
                    var validatedLines = new List<(SACH Book, int Qty, decimal LineTotal)>(itemsList.Count);

                    foreach (var cartItem in itemsList)
                    {
                        if (cartItem.SoLuongMua < 1 || cartItem.SoLuongMua > byte.MaxValue)
                        {
                            transaction.Rollback();
                            return OperationResult<DONHANG>.Failure($"Số lượng sách \"{cartItem.Book.Tensach}\" không hợp lệ.");
                        }

                        string bookKey = cartItem.Book.MaSach.Trim();
                        if (!dbBooksMap.TryGetValue(bookKey, out var dbBook))
                        {
                            transaction.Rollback();
                            return OperationResult<DONHANG>.Failure($"Sách \"{cartItem.Book.Tensach}\" không còn tồn tại trong hệ thống.");
                        }

                        int availableStock = dbBook.Soluongton ?? 0;
                        if (cartItem.SoLuongMua > availableStock)
                        {
                            transaction.Rollback();
                            return OperationResult<DONHANG>.Failure(
                                $"Sách \"{dbBook.Tensach}\" hiện chỉ còn {availableStock} cuốn.");
                        }

                        decimal unitPrice = dbBook.Giaban ?? cartItem.Book.Giaban ?? 0m;
                        decimal lineTotal = unitPrice * cartItem.SoLuongMua;
                        if (lineTotal > MaxSqlDecimal10_2)
                        {
                            transaction.Rollback();
                            return OperationResult<DONHANG>.Failure($"Thành tiền của sách \"{dbBook.Tensach}\" vượt quá giới hạn cho phép.");
                        }

                        grandTotal += lineTotal;
                        validatedLines.Add((dbBook, cartItem.SoLuongMua, lineTotal));
                    }

                    if (grandTotal > MaxSqlDecimal10_2)
                    {
                        transaction.Rollback();
                        return OperationResult<DONHANG>.Failure($"Tổng tiền đơn hàng {grandTotal:0} ₫ vượt quá giới hạn cho phép.");
                    }

                    DateTime now = DateTime.Now;
                    var order = new DONHANG
                    {
                        MaKH = customer.MaND,
                        NgayTao = DateOnly.FromDateTime(now),
                        NgayDat = now,
                        Method = method,
                        Status = OrderStatus.New,
                        Dathanhtoan = false,
                        Tongtien = grandTotal,
                        HoTenKhachHang = normalizedName,
                        SDTKhachHang = normalizedPhone,
                        DiaChiGiaoHang = normalizedAddress
                    };

                    db.DONHANGs.Add(order);
                    db.SaveChanges();

                    foreach (var (book, qty, lineTotal) in validatedLines)
                    {
                        var detail = new CHITIETDONHANG
                        {
                            MaDH = order.MaDH,
                            MaSach = book.MaSach,
                            Soluong = (byte)qty,
                            Tong = lineTotal,
                            TenSach = book.Tensach ?? string.Empty,
                            TacGia = book.Tacgia ?? string.Empty
                        };
                        db.CHITIETDONHANGs.Add(detail);
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    InvalidateCache();
                    order.MaKHNavigation = customer;
                    NotifyOrderUpdated(order);

                    return OperationResult<DONHANG>.Success(
                        order,
                        $"Đặt hàng thành công! Mã đơn hàng của bạn là {order.OrderCode}.");
                }
                catch (Exception innerEx)
                {
                    transaction.Rollback();
                    return OperationResult<DONHANG>.Failure(
                        $"Lỗi giao dịch: {innerEx.InnerException?.Message ?? innerEx.Message}");
                }
            }
            catch (Exception ex)
            {
                return OperationResult<DONHANG>.Failure(
                    $"Không thể kết nối: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        public OperationResult<DONHANG> ConfirmBankTransferPayment(int maDH)
        {
            try
            {
                using var db = new QuanlyNhaSachContext();
                using var transaction = db.Database.BeginTransaction();

                try
                {
                    var order = db.DONHANGs
                        .Include(o => o.MaKHNavigation)
                        .Include(o => o.CHITIETDONHANGs)
                            .ThenInclude(i => i.MaSachNavigation)
                        .Include(o => o.PHIEUXUATs)
                        .FirstOrDefault(o => o.MaDH == maDH);

                    if (order == null)
                    {
                        transaction.Rollback();
                        return OperationResult<DONHANG>.Failure($"Không tìm thấy đơn hàng #{maDH} trong cơ sở dữ liệu.");
                    }

                    if (order.Status == OrderStatus.Cancelled)
                    {
                        transaction.Rollback();
                        return OperationResult<DONHANG>.Failure($"Đơn hàng {order.OrderCode} đã hủy, không thể xác nhận thanh toán.");
                    }

                    order.Method = PaymentMethod.BankTransfer;
                    order.Dathanhtoan = true;
                    order.PaymentStatus = PaymentStatus.Paid;

                    if (order.Status == OrderStatus.New)
                    {
                        order.Status = OrderStatus.Confirmed;
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    InvalidateCache();
                    NotifyOrderUpdated(order);

                    return OperationResult<DONHANG>.Success(
                        order,
                        $"Đã xác nhận nhận tiền chuyển khoản ({order.FormattedTongTien}) và xác nhận đơn hàng {order.OrderCode} thành công!");
                }
                catch (Exception innerEx)
                {
                    transaction.Rollback();
                    return OperationResult<DONHANG>.Failure(
                        $"Lỗi khi xác nhận thanh toán chuyển khoản: {innerEx.InnerException?.Message ?? innerEx.Message}");
                }
            }
            catch (Exception ex)
            {
                return OperationResult<DONHANG>.Failure(
                    $"Không thể kết nối hệ thống: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        public OperationResult CancelPendingBankTransferOrder(int maDH)
        {
            try
            {
                using var db = new QuanlyNhaSachContext();
                using var transaction = db.Database.BeginTransaction();

                try
                {
                    var order = db.DONHANGs
                        .Include(o => o.CHITIETDONHANGs)
                        .FirstOrDefault(o => o.MaDH == maDH);

                    if (order == null)
                    {
                        transaction.Rollback();
                        return OperationResult.Success("Đơn hàng tạm không còn tồn tại.");
                    }

                    if (order.Dathanhtoan == true || order.Status != OrderStatus.New)
                    {
                        transaction.Rollback();
                        return OperationResult.Failure("Đơn hàng đã được thanh toán hoặc đang xử lý, không thể hủy phiên chờ QR.");
                    }

                    if (order.CHITIETDONHANGs != null && order.CHITIETDONHANGs.Count > 0)
                    {
                        db.CHITIETDONHANGs.RemoveRange(order.CHITIETDONHANGs);
                    }

                    db.DONHANGs.Remove(order);
                    db.SaveChanges();
                    transaction.Commit();

                    InvalidateCache();
                    OrderUpdated?.Invoke(this, null);
                    return OperationResult.Success($"Đã hủy phiên thanh toán chuyển khoản của đơn #{maDH:D4}.");
                }
                catch (Exception innerEx)
                {
                    transaction.Rollback();
                    return OperationResult.Failure(innerEx.InnerException?.Message ?? innerEx.Message);
                }
            }
            catch (Exception ex)
            {
                return OperationResult.Failure(ex.InnerException?.Message ?? ex.Message);
            }
        }

        public OperationResult<DONHANG> TransitionOrderStatusByLabel(int maDH, string? targetStatusLabel)
        {
            if (string.IsNullOrWhiteSpace(targetStatusLabel) ||
                !EnumExtensions.TryParseOrderStatusLabel(targetStatusLabel, out var targetStatus))
            {
                return OperationResult<DONHANG>.Failure("Trạng thái không hợp lệ.");
            }

            return TransitionOrderStatus(maDH, targetStatus);
        }

        public OperationResult<DONHANG> TransitionOrderStatus(int maDH, OrderStatus targetStatus)
        {
            try
            {
                using var db = new QuanlyNhaSachContext();
                using var transaction = db.Database.BeginTransaction();

                try
                {
                    var order = db.DONHANGs
                        .Include(o => o.MaKHNavigation)
                        .Include(o => o.CHITIETDONHANGs)
                            .ThenInclude(i => i.MaSachNavigation)
                        .Include(o => o.PHIEUXUATs)
                        .FirstOrDefault(o => o.MaDH == maDH);

                    if (order == null)
                    {
                        transaction.Rollback();
                        return OperationResult<DONHANG>.Failure($"Không tìm thấy đơn hàng #{maDH} trong cơ sở dữ liệu.");
                    }

                    OrderStatus currentStatus = order.Status;
                    if (currentStatus == targetStatus)
                    {
                        transaction.Rollback();
                        return OperationResult<DONHANG>.Failure(
                            $"Đơn hàng {order.OrderCode} hiện đã ở trạng thái \"{currentStatus.ToVietnameseLabel()}\".");
                    }

                    if (currentStatus == OrderStatus.Completed || currentStatus == OrderStatus.Cancelled)
                    {
                        transaction.Rollback();
                        return OperationResult<DONHANG>.Failure(
                            $"Đơn hàng không thể chuyển sang trạng thái khác.");
                    }

                    if (targetStatus == OrderStatus.Confirmed)
                    {
                        if (currentStatus != OrderStatus.New)
                        {
                            transaction.Rollback();
                            return OperationResult<DONHANG>.Failure(
                                $"Chỉ đơn hàng ở trạng thái \"Mới\" mới có thể chuyển sang \"Đã xác nhận\".");
                        }

                        if (order.Method == PaymentMethod.BankTransfer && order.PaymentStatus != PaymentStatus.Paid)
                        {
                            transaction.Rollback();
                            return OperationResult<DONHANG>.Failure(
                                $"Đơn hàng {order.OrderCode} chọn thanh toán Chuyển khoản nhưng chưa được xác nhận nhận tiền. Vui lòng kiểm tra tài khoản ngân hàng và bấm \"Duyệt CK\" (hoặc Thu tiền) trước khi xác nhận đơn hàng.");
                        }
                    }

                    if (targetStatus == OrderStatus.Shipping)
                    {
                        if (currentStatus != OrderStatus.Confirmed)
                        {
                            transaction.Rollback();
                            return OperationResult<DONHANG>.Failure(
                                $"Đơn hàng phải ở trạng thái \"Đã xác nhận\" trước khi chuyển sang \"Đang giao\".");
                        }

                        if (order.StockOutStatus != StockOutStatus.Issued)
                        {
                            transaction.Rollback();
                            return OperationResult<DONHANG>.Failure(
                                $"Đơn hàng chưa được xuất kho. Vui lòng thực hiện Xuất kho trước khi chuyển sang \"Đang giao\".");
                        }
                    }

                    if (targetStatus == OrderStatus.Completed)
                    {
                        if (currentStatus != OrderStatus.Shipping)
                        {
                            transaction.Rollback();
                            return OperationResult<DONHANG>.Failure(
                                $"Đơn hàng phải ở trạng thái \"Đang giao\" trước khi chuyển sang \"Hoàn tất\".");
                        }

                        if (order.PaymentStatus != PaymentStatus.Paid)
                        {
                            transaction.Rollback();
                            return OperationResult<DONHANG>.Failure(
                                $"Đơn hàng chưa được thanh toán. Vui lòng ghi nhận Thu tiền trước khi hoàn tất đơn hàng.");
                        }
                    }

                    if (targetStatus == OrderStatus.Cancelled)
                    {
                        if (currentStatus == OrderStatus.Shipping)
                        {
                            transaction.Rollback();
                            return OperationResult<DONHANG>.Failure(
                                $"Đơn hàng {order.OrderCode} đang trong quá trình vận chuyển, không thể hủy trực tiếp.");
                        }

                        if (order.PHIEUXUATs != null && order.PHIEUXUATs.Count > 0 && order.CHITIETDONHANGs != null)
                        {
                            var bookKeys = order.CHITIETDONHANGs.Select(l => l.MaSach.Trim()).Distinct().ToList();
                            var trackedBooks = db.SACHs
                                .Where(b => bookKeys.Contains(b.MaSach))
                                .ToDictionary(b => b.MaSach.Trim(), b => b, StringComparer.OrdinalIgnoreCase);

                            foreach (var line in order.CHITIETDONHANGs)
                            {
                                if (trackedBooks.TryGetValue(line.MaSach.Trim(), out var book))
                                {
                                    int restored = Math.Min(byte.MaxValue, (book.Soluongton ?? 0) + line.SoLuongMua);
                                    book.Soluongton = (byte)restored;
                                }
                            }

                            db.PHIEUXUATs.RemoveRange(order.PHIEUXUATs);
                        }
                        bool wasPaid = order.Dathanhtoan == true ||
                                       order.PaymentStatus == PaymentStatus.Paid ||
                                       order.PaymentStatus == PaymentStatus.Refunded;
                        order.Status = targetStatus;
                        order.PaymentStatus = wasPaid ? PaymentStatus.Refunded : PaymentStatus.Pending;
                    }
                    else
                    {
                        order.Status = targetStatus;
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    InvalidateCache();
                    NotifyOrderUpdated(order);

                    string refundNote = (targetStatus == OrderStatus.Cancelled && order.PaymentStatus == PaymentStatus.Refunded)
                        ? " Trạng thái thanh toán đã chuyển sang \"Hoàn tiền\"."
                        : string.Empty;

                    return OperationResult<DONHANG>.Success(
                        order,
                        $"Đã chuyển trạng thái đơn hàng {order.OrderCode} từ \"{currentStatus.ToVietnameseLabel()}\" sang \"{targetStatus.ToVietnameseLabel()}\" thành công.{refundNote}");
                }
                catch (Exception innerEx)
                {
                    transaction.Rollback();
                    return OperationResult<DONHANG>.Failure(
                        $"Lỗi giao dịch khi cập nhật trạng thái đơn hàng: {innerEx.InnerException?.Message ?? innerEx.Message}");
                }
            }
            catch (Exception ex)
            {
                return OperationResult<DONHANG>.Failure(
                    $"Không thể kết nối hệ thống: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        public OperationResult<DONHANG> RecordPayment(
            int maDH,
            PaymentMethod selectedMethod,
            decimal? enteredAmount,
            bool confirmCodCollectionOnDelivery = false)
        {
            try
            {
                using var db = new QuanlyNhaSachContext();
                using var transaction = db.Database.BeginTransaction();

                try
                {
                    var order = db.DONHANGs
                        .Include(o => o.MaKHNavigation)
                        .Include(o => o.CHITIETDONHANGs)
                            .ThenInclude(i => i.MaSachNavigation)
                        .Include(o => o.PHIEUXUATs)
                        .FirstOrDefault(o => o.MaDH == maDH);

                    if (order == null)
                    {
                        transaction.Rollback();
                        return OperationResult<DONHANG>.Failure($"Không tìm thấy đơn hàng #{maDH} trong hệ thống.");
                    }
                    if (order.Status == OrderStatus.Cancelled)
                    {
                        transaction.Rollback();
                        return OperationResult<DONHANG>.Failure($"Đơn hàng {order.OrderCode} đã hủy, không thể ghi nhận thanh toán.");
                    }

                    if (order.PaymentStatus == PaymentStatus.Paid)
                    {
                        transaction.Rollback();
                        return OperationResult<DONHANG>.Failure($"Đơn hàng {order.OrderCode} đã được thanh toán trước đó.");
                    }

                    decimal requiredTotal = order.TongTien;

                    if (selectedMethod == PaymentMethod.COD && !confirmCodCollectionOnDelivery)
                    {
                        order.Method = PaymentMethod.COD;
                        order.Dathanhtoan = false;
                        order.PaymentStatus = PaymentStatus.PayOnDelivery;

                        db.SaveChanges();
                        transaction.Commit();

                        InvalidateCache();
                        NotifyOrderUpdated(order);
                        return OperationResult<DONHANG>.Success(
                            order,
                            $"Đã cập nhật phương thức thanh toán đơn hàng {order.OrderCode} sang Thu hộ (COD).");
                    }

                    if (!enteredAmount.HasValue || enteredAmount.Value < requiredTotal)
                    {
                        transaction.Rollback();
                        return OperationResult<DONHANG>.Failure(
                            $"Số tiền thanh toán không được nhỏ hơn tổng tiền đơn hàng.");
                    }

                    order.Method = selectedMethod;
                    order.Dathanhtoan = true;
                    order.PaymentStatus = PaymentStatus.Paid;

                    string extraStatusNote = string.Empty;
                    if (selectedMethod == PaymentMethod.BankTransfer && order.Status == OrderStatus.New)
                    {
                        order.Status = OrderStatus.Confirmed;
                        extraStatusNote = " Đơn hàng đã được tự động chuyển sang trạng thái Đã xác nhận.";
                    }
                    else if (confirmCodCollectionOnDelivery && order.Status == OrderStatus.Shipping)
                    {
                        order.Status = OrderStatus.Completed;
                        extraStatusNote = " Đơn hàng đã được chuyển sang trạng thái Hoàn tất.";
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    decimal changeAmount = Math.Max(0m, enteredAmount.Value - requiredTotal);
                    string changeNote = changeAmount > 0m ? $" (Tiền thừa: {changeAmount:N0} ₫)." : ".";

                    InvalidateCache();
                    NotifyOrderUpdated(order);
                    return OperationResult<DONHANG>.Success(
                        order,
                        $"Đã ghi nhận thanh toán {selectedMethod.ToVietnameseLabel()} cho đơn hàng {order.OrderCode} với số tiền {enteredAmount.Value:N0} ₫{changeNote}{extraStatusNote}");
                }
                catch (Exception innerEx)
                {
                    transaction.Rollback();
                    return OperationResult<DONHANG>.Failure(
                        $"Lỗi giao dịch khi ghi nhận thanh toán: {innerEx.InnerException?.Message ?? innerEx.Message}");
                }
            }
            catch (Exception ex)
            {
                return OperationResult<DONHANG>.Failure(
                    $"Không thể kết nối hệ thống: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private static string GenerateNextCustomerId(QuanlyNhaSachContext db)
        {
            var existingIds = db.NGUOIDUNGs
                .AsNoTracking()
                .Select(u => u.MaND)
                .ToList();

            int maxNum = 0;
            foreach (var id in existingIds)
            {
                if (string.IsNullOrWhiteSpace(id) || !id.StartsWith("KH", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string digits = Regex.Replace(id, @"\D", "");
                if (int.TryParse(digits, out int num) && num > maxNum)
                {
                    maxNum = num;
                }
            }

            int next = maxNum + 1;
            string candidate = $"KH{next:D3}";
            while (existingIds.Any(x => string.Equals(x?.Trim(), candidate, StringComparison.OrdinalIgnoreCase)))
            {
                next++;
                candidate = $"KH{next:D3}";
            }

            return candidate;
        }

        private void NotifyOrderUpdated(DONHANG order)
        {
            OrderUpdated?.Invoke(this, order);
        }
    }
}
