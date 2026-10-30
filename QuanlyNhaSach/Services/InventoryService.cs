using System;
using System.Collections.Generic;
using System.Linq;
using QuanlyNhaSach.Models;
using QuanlyNhaSach.Models.Enums;

namespace QuanlyNhaSach.Services
{
    public class InventoryService
    {
        private readonly MockDataService _mockDataService;

        public event EventHandler? InventoryChanged;

        public InventoryService(MockDataService mockDataService)
        {
            _mockDataService = mockDataService ?? throw new ArgumentNullException(nameof(mockDataService));
        }

        public int GetCurrentStock(int maSach)
        {
            var book = _mockDataService.GetBookById(maSach);
            return book?.SoLuongTon ?? 0;
        }

        public OperationResult ValidateRequestedQuantity(int maSach, int requestedQuantity)
        {
            if (requestedQuantity < 1)
            {
                return OperationResult.Failure("Số lượng yêu cầu phải lớn hơn hoặc bằng 1.");
            }

            var book = _mockDataService.GetBookById(maSach);
            if (book == null)
            {
                return OperationResult.Failure($"Không tìm thấy sách có mã #{maSach} trong hệ thống.");
            }

            if (book.SoLuongTon < requestedQuantity)
            {
                return OperationResult.Failure(
                    $"Thiếu tồn kho cho sách \"{book.TenSach}\": Cần {requestedQuantity} cuốn, tồn kho hiện tại chỉ còn {book.SoLuongTon} cuốn.");
            }

            return OperationResult.Success();
        }

        public IReadOnlyList<StockOutLineItem> GetStockOutLineItems(Order? order)
        {
            if (order == null || order.Items == null || order.Items.Count == 0)
            {
                return Array.Empty<StockOutLineItem>();
            }

            bool isIssued = order.StockOutStatus == StockOutStatus.Issued;
            var result = new List<StockOutLineItem>(order.Items.Count);

            foreach (var item in order.Items)
            {
                var sharedBook = _mockDataService.GetBookById(item.MaSach);
                result.Add(new StockOutLineItem(order.MaDH, item, sharedBook, isIssued));
            }

            // Synchronize order's StockOutStatus if not already issued
            if (!isIssued && order.Status != OrderStatus.Cancelled)
            {
                bool anyInsufficient = result.Any(r => r.IsInsufficientStock);
                order.StockOutStatus = anyInsufficient
                    ? StockOutStatus.InsufficientStock
                    : StockOutStatus.NotIssued;
            }

            return result;
        }

        public OperationResult ValidateStockOut(Order? order)
        {
            if (order == null)
            {
                return OperationResult.Failure("Không tìm thấy đơn hàng cần xuất kho.");
            }

            if (order.Status == OrderStatus.Cancelled)
            {
                return OperationResult.Failure($"Đơn hàng #{order.MaDH} đã hủy. Không được phép thực hiện xuất kho.");
            }

            if (order.StockOutStatus == StockOutStatus.Issued ||
                order.Status == OrderStatus.Shipping ||
                order.Status == OrderStatus.Completed)
            {
                return OperationResult.Failure(
                    $"Đơn hàng #{order.MaDH} đã được xuất kho trước đó. Hệ thống từ chối xuất kho trùng lặp.");
            }

            if (order.Items == null || order.Items.Count == 0)
            {
                return OperationResult.Failure($"Đơn hàng #{order.MaDH} không có danh mục sách cần xuất.");
            }

            var insufficientDetails = new List<string>();

            var groupedItems = order.Items
                .GroupBy(i => i.MaSach)
                .Select(g => new
                {
                    MaSach = g.Key,
                    TenSach = g.First().TenSach,
                    RequiredQty = g.Sum(x => x.SoLuongMua)
                })
                .ToList();

            foreach (var line in groupedItems)
            {
                if (line.RequiredQty < 1)
                {
                    return OperationResult.Failure($"Số lượng xuất của sách \"{line.TenSach}\" không hợp lệ ({line.RequiredQty}).");
                }

                var sharedBook = _mockDataService.GetBookById(line.MaSach);
                if (sharedBook == null)
                {
                    return OperationResult.Failure($"Sách \"{line.TenSach}\" (Mã #{line.MaSach}) không tồn tại trong kho.");
                }

                if (sharedBook.SoLuongTon < line.RequiredQty)
                {
                    int shortage = line.RequiredQty - sharedBook.SoLuongTon;
                    insufficientDetails.Add(
                        $"• {sharedBook.TenSach} (Mã #{sharedBook.MaSach}): Cần xuất {line.RequiredQty} cuốn, tồn kho hiện tại {sharedBook.SoLuongTon} cuốn (Thiếu {shortage} cuốn)");
                }
            }

            if (insufficientDetails.Count > 0)
            {
                order.StockOutStatus = StockOutStatus.InsufficientStock;
                order.NgayCapNhat = DateTime.Now;
                _mockDataService.NotifyDataChanged();

                string message =
                    $"Không thể xác nhận xuất kho cho đơn hàng #{order.MaDH} do THIẾU TỒN KHO:\n" +
                    string.Join("\n", insufficientDetails) +
                    "\n\nTồn kho được giữ nguyên, không có cuốn sách nào bị trừ.";

                return OperationResult.Failure(message);
            }

            return OperationResult.Success("Đơn hàng đủ điều kiện tồn kho để xuất kho.");
        }

        public OperationResult<Order> ExecuteStockOut(int maDH)
        {
            var order = _mockDataService.GetOrderById(maDH);
            if (order == null)
            {
                return OperationResult<Order>.Failure($"Không tìm thấy đơn hàng có mã #{maDH} trong hệ thống.");
            }

            return ExecuteStockOut(order);
        }

        public OperationResult<Order> ExecuteStockOut(Order? order)
        {
            if (order == null)
            {
                return OperationResult<Order>.Failure("Vui lòng chọn đơn hàng hợp lệ trước khi xác nhận xuất kho.");
            }

            // 1. Validate all lines atomically before changing any Book stock
            var validation = ValidateStockOut(order);
            if (!validation.IsSuccess)
            {
                return OperationResult<Order>.Failure(validation.Message);
            }

            // 2. Prepare atomic deductions
            var deductions = new List<(Book SharedBook, int Quantity)>();
            foreach (var item in order.Items)
            {
                var sharedBook = _mockDataService.GetBookById(item.MaSach);
                if (sharedBook == null || sharedBook.SoLuongTon < item.SoLuongMua)
                {
                    order.StockOutStatus = StockOutStatus.InsufficientStock;
                    return OperationResult<Order>.Failure(
                        $"Phát hiện thiếu tồn kho cho sách \"{item.TenSach}\". Không thực hiện trừ kho.");
                }
                deductions.Add((sharedBook, item.SoLuongMua));
            }

            // 3. Apply stock deductions across all lines
            int totalBooksIssued = 0;
            foreach (var (sharedBook, quantity) in deductions)
            {
                sharedBook.SoLuongTon -= quantity;
                totalBooksIssued += quantity;
            }

            // 4. Mark order StockOutStatus as Issued
            order.StockOutStatus = StockOutStatus.Issued;
            order.StockOutDate = DateTime.Now;
            order.NgayCapNhat = DateTime.Now;

            if (order.Status == OrderStatus.New)
            {
                order.Status = OrderStatus.Confirmed;
            }

            InventoryChanged?.Invoke(this, EventArgs.Empty);
            _mockDataService.NotifyDataChanged();

            string summaryMessage =
                $"Xuất kho thành công cho đơn hàng #{order.MaDH}!\n" +
                $"• Khách hàng: {order.HoTenKhachHang}\n" +
                $"• Tổng số sách đã xuất: {totalBooksIssued} cuốn ({order.Items.Count} đầu sách)\n" +
                $"• Trạng thái xuất kho: {order.TrangThaiXuatKho}\n" +
                $"• Số lượng tồn kho đã được cập nhật đồng bộ trên toàn hệ thống.";

            return OperationResult<Order>.Success(order, summaryMessage);
        }
    }
}
