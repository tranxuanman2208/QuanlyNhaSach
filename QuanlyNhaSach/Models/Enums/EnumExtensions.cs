using System;

namespace QuanlyNhaSach.Models.Enums
{
    public static class EnumExtensions
    {
        public static string ToVietnameseLabel(this UserRole role) => role switch
        {
            UserRole.Customer => "Khách hàng",
            UserRole.Staff => "Nhân viên",
            UserRole.Manager => "Quản lý",
            _ => "Không xác định"
        };

        public static string ToVietnameseLabel(this AccountStatus status) => status switch
        {
            AccountStatus.Active => "Hoạt động",
            AccountStatus.Locked => "Đã khóa",
            _ => "Hoạt động"
        };

        public static string ToVietnameseLabel(this ImportReceiptStatus status) => status switch
        {
            ImportReceiptStatus.Draft => "Bản nháp",
            ImportReceiptStatus.Confirmed => "Đã xác nhận",
            ImportReceiptStatus.Cancelled => "Đã hủy",
            _ => "Đã xác nhận"
        };

        public static string ToVietnameseLabel(this OrderStatus status) => status switch
        {
            OrderStatus.New => "Mới",
            OrderStatus.Confirmed => "Đã xác nhận",
            OrderStatus.Shipping => "Đang giao",
            OrderStatus.Completed => "Hoàn tất",
            OrderStatus.Cancelled => "Đã hủy",
            _ => "Mới"
        };

        public static string ToVietnameseLabel(this PaymentMethod method) => method switch
        {
            PaymentMethod.Cash => "Tiền mặt",
            PaymentMethod.BankTransfer => "Chuyển khoản",
            PaymentMethod.COD => "COD",
            _ => "COD"
        };

        public static string ToVietnameseLabel(this PaymentStatus status) => status switch
        {
            PaymentStatus.Pending => "Chưa thanh toán",
            PaymentStatus.Paid => "Đã thanh toán",
            PaymentStatus.PayOnDelivery => "Thu hộ (COD)",
            PaymentStatus.Failed => "Thanh toán thất bại",
            _ => "Chưa thanh toán"
        };

        public static string ToVietnameseLabel(this StockOutStatus status) => status switch
        {
            StockOutStatus.NotIssued => "Chưa xuất kho",
            StockOutStatus.Issued => "Đã xuất kho",
            StockOutStatus.InsufficientStock => "Thiếu tồn kho",
            _ => "Chưa xuất kho"
        };

        public static PaymentMethod ParsePaymentMethod(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return PaymentMethod.COD;
            }

            string trimmed = input.Trim();
            if (trimmed.Contains("Tiền mặt", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("Cash", StringComparison.OrdinalIgnoreCase))
            {
                return PaymentMethod.Cash;
            }

            if (trimmed.Contains("Chuyển khoản", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("BankTransfer", StringComparison.OrdinalIgnoreCase))
            {
                return PaymentMethod.BankTransfer;
            }

            return PaymentMethod.COD;
        }

        public static bool TryParseOrderStatusLabel(string? label, out OrderStatus status)
        {
            status = OrderStatus.New;
            if (string.IsNullOrWhiteSpace(label))
            {
                return false;
            }

            string trimmed = label.Trim();
            if (trimmed.Equals("Mới", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("Đã tiếp nhận", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("New", StringComparison.OrdinalIgnoreCase))
            {
                status = OrderStatus.New;
                return true;
            }

            if (trimmed.Equals("Đã xác nhận", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("Confirmed", StringComparison.OrdinalIgnoreCase))
            {
                status = OrderStatus.Confirmed;
                return true;
            }

            if (trimmed.Equals("Đang giao", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("Shipping", StringComparison.OrdinalIgnoreCase))
            {
                status = OrderStatus.Shipping;
                return true;
            }

            if (trimmed.Equals("Hoàn tất", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("Completed", StringComparison.OrdinalIgnoreCase))
            {
                status = OrderStatus.Completed;
                return true;
            }

            if (trimmed.Equals("Đã hủy", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                status = OrderStatus.Cancelled;
                return true;
            }

            return false;
        }
    }
}
