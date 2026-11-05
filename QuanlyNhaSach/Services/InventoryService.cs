using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using QuanlyNhaSach.Models;
using QuanlyNhaSach.Models.Enums;

namespace QuanlyNhaSach.Services
{
    public class InventoryService
    {
        public event EventHandler? InventoryChanged;

        public InventoryService()
        {
        }

        public int GetCurrentStock(string? maSach)
        {
            if (string.IsNullOrWhiteSpace(maSach))
            {
                return 0;
            }

            try
            {
                string key = maSach.Trim();
                using var db = new QuanlyNhaSachContext();
                var book = db.SACHs
                    .AsNoTracking()
                    .FirstOrDefault(b => b.MaSach == key && (b.Daxoa == null || b.Daxoa == false));
                return book?.Soluongton ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        public OperationResult ValidateRequestedQuantity(string? maSach, int requestedQuantity)
        {
            if (requestedQuantity < 1)
            {
                return OperationResult.Failure("Số lượng yêu cầu phải lớn hơn hoặc bằng 1.");
            }

            if (string.IsNullOrWhiteSpace(maSach))
            {
                return OperationResult.Failure("Mã sách không hợp lệ.");
            }

            try
            {
                string key = maSach.Trim();
                using var db = new QuanlyNhaSachContext();
                var book = db.SACHs
                    .AsNoTracking()
                    .FirstOrDefault(b => b.MaSach == key && (b.Daxoa == null || b.Daxoa == false));

                if (book == null)
                {
                    return OperationResult.Failure($"Không tìm thấy sách có mã #{key} trong kho.");
                }

                int currentStock = book.Soluongton ?? 0;
                if (currentStock < requestedQuantity)
                {
                    return OperationResult.Failure(
                        $"Sách \"{book.Tensach}\" hiện chỉ còn {currentStock} cuốn trong kho (yêu cầu: {requestedQuantity} cuốn).");
                }

                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                return OperationResult.Failure($"Không thể kiểm tra tồn kho: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        public IReadOnlyList<StockOutLineItem> GetStockOutLineItems(DONHANG? order)
        {
            if (order == null)
            {
                return Array.Empty<StockOutLineItem>();
            }

            try
            {
                using var db = new QuanlyNhaSachContext();
                var dbOrder = db.DONHANGs
                    .AsNoTracking()
                    .Include(o => o.CHITIETDONHANGs)
                        .ThenInclude(i => i.MaSachNavigation)
                    .Include(o => o.PHIEUXUATs)
                    .FirstOrDefault(o => o.MaDH == order.MaDH) ?? order;

                var items = dbOrder.CHITIETDONHANGs?.ToList() ?? new List<CHITIETDONHANG>();
                if (items.Count == 0)
                {
                    return Array.Empty<StockOutLineItem>();
                }

                bool isAlreadyIssued = dbOrder.StockOutStatus == StockOutStatus.Issued;
                var bookIds = items.Select(i => i.MaSach).Distinct().ToList();
                var booksMap = db.SACHs
                    .AsNoTracking()
                    .Where(b => bookIds.Contains(b.MaSach))
                    .ToDictionary(b => b.MaSach.Trim(), b => b, StringComparer.OrdinalIgnoreCase);

                var result = new List<StockOutLineItem>(items.Count);
                foreach (var item in items)
                {
                    booksMap.TryGetValue(item.MaSach?.Trim() ?? string.Empty, out var book);
                    book ??= item.MaSachNavigation;
                    result.Add(new StockOutLineItem(dbOrder.MaDH, item, book, isAlreadyIssued));
                }

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi tải chi tiết xuất kho: {ex.Message}");
                return Array.Empty<StockOutLineItem>();
            }
        }

        public OperationResult ValidateStockOut(DONHANG? order)
        {
            if (order == null)
            {
                return OperationResult.Failure("Không tìm thấy đơn hàng cần xuất kho.");
            }

            try
            {
                using var db = new QuanlyNhaSachContext();
                var dbOrder = db.DONHANGs
                    .AsNoTracking()
                    .Include(o => o.CHITIETDONHANGs)
                        .ThenInclude(i => i.MaSachNavigation)
                    .Include(o => o.PHIEUXUATs)
                    .FirstOrDefault(o => o.MaDH == order.MaDH);

                if (dbOrder == null)
                {
                    return OperationResult.Failure($"Không tìm thấy đơn hàng #{order.MaDH} trong cơ sở dữ liệu.");
                }

                if (dbOrder.Status == OrderStatus.Cancelled)
                {
                    return OperationResult.Failure($"Đơn hàng {dbOrder.OrderCode} đã bị hủy, không thể xuất kho.");
                }

                if (dbOrder.StockOutStatus == StockOutStatus.Issued)
                {
                    return OperationResult.Failure($"Đơn hàng {dbOrder.OrderCode} đã được xuất kho trước đó.");
                }

                if (dbOrder.CHITIETDONHANGs == null || dbOrder.CHITIETDONHANGs.Count == 0)
                {
                    return OperationResult.Failure($"Đơn hàng {dbOrder.OrderCode} không có chi tiết sách cần xuất.");
                }

                foreach (var item in dbOrder.CHITIETDONHANGs)
                {
                    string bookKey = item.MaSach.Trim();
                    var book = db.SACHs
                        .AsNoTracking()
                        .FirstOrDefault(b => b.MaSach == bookKey && (b.Daxoa == null || b.Daxoa == false));

                    if (book == null)
                    {
                        return OperationResult.Failure(
                            $"Sách \"{item.TenSach}\" (Mã #{bookKey}) không tồn tại hoặc đã ngừng kinh doanh.");
                    }

                    int currentStock = book.Soluongton ?? 0;
                    int requiredQty = item.SoLuongMua;
                    if (currentStock < requiredQty)
                    {
                        return OperationResult.Failure(
                            $"Thiếu tồn kho cho sách \"{book.Tensach}\" (Mã #{book.MaSach}): Cần xuất {requiredQty} cuốn nhưng kho chỉ còn {currentStock} cuốn.");
                    }
                }

                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                return OperationResult.Failure(
                    $"Không thể kiểm tra điều kiện xuất kho: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        public OperationResult<DONHANG> ExecuteStockOut(int maDH)
        {
            try
            {
                using var db = new QuanlyNhaSachContext();
                var order = db.DONHANGs
                    .AsNoTracking()
                    .FirstOrDefault(o => o.MaDH == maDH);

                if (order == null)
                {
                    return OperationResult<DONHANG>.Failure($"Không tìm thấy đơn hàng #{maDH} trong cơ sở dữ liệu.");
                }

                return ExecuteStockOut(order);
            }
            catch (Exception ex)
            {
                return OperationResult<DONHANG>.Failure(
                    $"Lỗi khi truy vấn đơn hàng #{maDH}: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        public OperationResult<DONHANG> ExecuteStockOut(DONHANG? order)
        {
            if (order == null)
            {
                return OperationResult<DONHANG>.Failure("Vui lòng chọn đơn hàng hợp lệ trước khi xác nhận xuất kho.");
            }

            try
            {
                using var db = new QuanlyNhaSachContext();
                using var transaction = db.Database.BeginTransaction();

                try
                {
                    var dbOrder = db.DONHANGs
                        .Include(o => o.MaKHNavigation)
                        .Include(o => o.CHITIETDONHANGs)
                            .ThenInclude(i => i.MaSachNavigation)
                        .Include(o => o.PHIEUXUATs)
                        .FirstOrDefault(o => o.MaDH == order.MaDH);

                    if (dbOrder == null)
                    {
                        transaction.Rollback();
                        return OperationResult<DONHANG>.Failure($"Không tìm thấy đơn hàng #{order.MaDH} trong cơ sở dữ liệu.");
                    }

                    if (dbOrder.Status == OrderStatus.Cancelled)
                    {
                        transaction.Rollback();
                        return OperationResult<DONHANG>.Failure($"Đơn hàng {dbOrder.OrderCode} đã hủy, không thể xuất kho.");
                    }

                    if (dbOrder.StockOutStatus == StockOutStatus.Issued)
                    {
                        transaction.Rollback();
                        return OperationResult<DONHANG>.Failure($"Đơn hàng {dbOrder.OrderCode} đã được xuất kho trước đó (không trừ kho lặp lại).");
                    }

                    if (dbOrder.CHITIETDONHANGs == null || dbOrder.CHITIETDONHANGs.Count == 0)
                    {
                        transaction.Rollback();
                        return OperationResult<DONHANG>.Failure($"Đơn hàng {dbOrder.OrderCode} không có sách để xuất kho.");
                    }

                    int totalIssuedBooks = 0;

                    foreach (var line in dbOrder.CHITIETDONHANGs)
                    {
                        string bookKey = line.MaSach.Trim();
                        var book = db.SACHs.FirstOrDefault(b => b.MaSach == bookKey && (b.Daxoa == null || b.Daxoa == false));
                        if (book == null)
                        {
                            transaction.Rollback();
                            return OperationResult<DONHANG>.Failure(
                                $"Không tìm thấy sách \"{line.TenSach}\" (Mã #{bookKey}) trong kho.");
                        }

                        int currentStock = book.Soluongton ?? 0;
                        int issueQty = line.SoLuongMua;
                        if (issueQty < 1)
                        {
                            continue;
                        }

                        if (currentStock < issueQty)
                        {
                            transaction.Rollback();
                            return OperationResult<DONHANG>.Failure(
                                $"Không đủ tồn kho cho sách \"{book.Tensach}\" (Mã #{book.MaSach}): Cần xuất {issueQty} cuốn, tồn hiện tại {currentStock} cuốn.");
                        }

                        book.Soluongton = (byte)(currentStock - issueQty);
                        totalIssuedBooks += issueQty;
                    }

                    if (dbOrder.Status == OrderStatus.New)
                    {
                        dbOrder.Status = OrderStatus.Confirmed;
                    }

                    var phieuXuat = new PHIEUXUAT
                    {
                        MaDH = dbOrder.MaDH,
                        NgayXuat = DateOnly.FromDateTime(DateTime.Now),
                        Ghichu = $"Xuất kho {totalIssuedBooks} cuốn cho đơn hàng {dbOrder.OrderCode}"
                    };

                    db.PHIEUXUATs.Add(phieuXuat);
                    db.SaveChanges();
                    transaction.Commit();

                    dbOrder.StockOutStatus = StockOutStatus.Issued;
                    dbOrder.StockOutDate = DateTime.Now;
                    order.StockOutStatus = StockOutStatus.Issued;
                    order.StockOutDate = DateTime.Now;
                    order.Status = dbOrder.Status;

                    InventoryChanged?.Invoke(this, EventArgs.Empty);

                    return OperationResult<DONHANG>.Success(
                        dbOrder,
                        $"Đã xác nhận xuất kho thành công cho đơn hàng {dbOrder.OrderCode} (Phiếu xuất #{phieuXuat.MaPX}, tổng {totalIssuedBooks} cuốn). Tồn kho đã được trừ tương ứng.");
                }
                catch (Exception innerEx)
                {
                    transaction.Rollback();
                    return OperationResult<DONHANG>.Failure(
                        $"Lỗi giao dịch khi xuất kho: {innerEx.InnerException?.Message ?? innerEx.Message}");
                }
            }
            catch (Exception ex)
            {
                return OperationResult<DONHANG>.Failure(
                    $"Không thể kết nối cơ sở dữ liệu để xuất kho: {ex.InnerException?.Message ?? ex.Message}");
            }
        }
    }
}
