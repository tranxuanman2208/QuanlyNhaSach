using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using QuanlyNhaSach.Models.Enums;

namespace QuanlyNhaSach.Converters
{
    public class CurrencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                decimal d => $"{d:N0} ₫",
                double db => $"{db:N0} ₫",
                int i => $"{i:N0} ₫",
                _ => "0 ₫"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class BooleanToInverseVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return b ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class StringToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hex && !string.IsNullOrWhiteSpace(hex))
            {
                try
                {
                    if (ColorConverter.ConvertFromString(hex) is Color color)
                    {
                        var brush = new SolidColorBrush(color);
                        brush.Freeze();
                        return brush;
                    }
                }
                catch
                {
                    // Fallback to brand dark blue
                }
            }

            var fallback = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E3A8A"));
            fallback.Freeze();
            return fallback;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class StockBadgeBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int stock)
            {
                if (stock <= 0)
                {
                    return CreateBrush("#FEE2E2");
                }
                if (stock < 10)
                {
                    return CreateBrush("#FEF2F2");
                }
            }
            return CreateBrush("#ECFDF5");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();

        private static SolidColorBrush CreateBrush(string hex)
        {
            var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            b.Freeze();
            return b;
        }
    }

    public class StockBadgeForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int stock)
            {
                if (stock <= 0)
                {
                    return CreateBrush("#991B1B");
                }
                if (stock < 10)
                {
                    return CreateBrush("#DC2626");
                }
            }
            return CreateBrush("#059669");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();

        private static SolidColorBrush CreateBrush(string hex)
        {
            var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            b.Freeze();
            return b;
        }
    }

    public class AccountStatusBadgeBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isActive = value switch
            {
                bool b => b,
                AccountStatus status => status == AccountStatus.Active,
                string s => s.Equals("Hoạt động", StringComparison.OrdinalIgnoreCase),
                _ => true
            };

            return isActive ? CreateBrush("#DCFCE7") : CreateBrush("#FEE2E2");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();

        private static SolidColorBrush CreateBrush(string hex)
        {
            var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            b.Freeze();
            return b;
        }
    }

    public class AccountStatusBadgeForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isActive = value switch
            {
                bool b => b,
                AccountStatus status => status == AccountStatus.Active,
                string s => s.Equals("Hoạt động", StringComparison.OrdinalIgnoreCase),
                _ => true
            };

            return isActive ? CreateBrush("#15803D") : CreateBrush("#DC2626");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();

        private static SolidColorBrush CreateBrush(string hex)
        {
            var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            b.Freeze();
            return b;
        }
    }

    public class OrderStatusBadgeBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string label = value switch
            {
                OrderStatus os => os.ToVietnameseLabel(),
                string s => s,
                _ => string.Empty
            };

            return label switch
            {
                "Mới" => CreateBrush("#DBEAFE"),
                "Đã xác nhận" => CreateBrush("#E0E7FF"),
                "Đang giao" => CreateBrush("#FEF3C7"),
                "Hoàn tất" => CreateBrush("#DCFCE7"),
                "Đã hủy" => CreateBrush("#FEE2E2"),
                _ => CreateBrush("#F1F5F9")
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();

        private static SolidColorBrush CreateBrush(string hex)
        {
            var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            b.Freeze();
            return b;
        }
    }

    public class OrderStatusBadgeForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string label = value switch
            {
                OrderStatus os => os.ToVietnameseLabel(),
                string s => s,
                _ => string.Empty
            };

            return label switch
            {
                "Mới" => CreateBrush("#1E40AF"),
                "Đã xác nhận" => CreateBrush("#3730A3"),
                "Đang giao" => CreateBrush("#B45309"),
                "Hoàn tất" => CreateBrush("#15803D"),
                "Đã hủy" => CreateBrush("#DC2626"),
                _ => CreateBrush("#475569")
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();

        private static SolidColorBrush CreateBrush(string hex)
        {
            var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            b.Freeze();
            return b;
        }
    }

    public class PaymentStatusBadgeBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string label = value switch
            {
                PaymentStatus ps => ps.ToVietnameseLabel(),
                string s => s,
                _ => string.Empty
            };

            return label switch
            {
                "Đã thanh toán" => CreateBrush("#DCFCE7"),
                "Thu hộ (COD)" => CreateBrush("#FFF7ED"),
                "Chưa thanh toán" => CreateBrush("#FEF9C3"),
                "Thanh toán thất bại" => CreateBrush("#FEE2E2"),
                _ => CreateBrush("#F1F5F9")
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();

        private static SolidColorBrush CreateBrush(string hex)
        {
            var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            b.Freeze();
            return b;
        }
    }

    public class PaymentStatusBadgeForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string label = value switch
            {
                PaymentStatus ps => ps.ToVietnameseLabel(),
                string s => s,
                _ => string.Empty
            };

            return label switch
            {
                "Đã thanh toán" => CreateBrush("#15803D"),
                "Thu hộ (COD)" => CreateBrush("#C2410C"),
                "Chưa thanh toán" => CreateBrush("#A16207"),
                "Thanh toán thất bại" => CreateBrush("#DC2626"),
                _ => CreateBrush("#475569")
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();

        private static SolidColorBrush CreateBrush(string hex)
        {
            var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            b.Freeze();
            return b;
        }
    }

    public class StockOutStatusBadgeBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string label = value switch
            {
                StockOutStatus ss => ss.ToVietnameseLabel(),
                string s => s,
                _ => string.Empty
            };

            return label switch
            {
                "Đã xuất kho" or "Đủ tồn kho" => CreateBrush("#DCFCE7"),
                "Thiếu tồn kho" => CreateBrush("#FEE2E2"),
                _ => CreateBrush("#F1F5F9")
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();

        private static SolidColorBrush CreateBrush(string hex)
        {
            var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            b.Freeze();
            return b;
        }
    }

    public class StockOutStatusBadgeForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string label = value switch
            {
                StockOutStatus ss => ss.ToVietnameseLabel(),
                string s => s,
                _ => string.Empty
            };

            return label switch
            {
                "Đã xuất kho" or "Đủ tồn kho" => CreateBrush("#15803D"),
                "Thiếu tồn kho" => CreateBrush("#DC2626"),
                _ => CreateBrush("#475569")
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();

        private static SolidColorBrush CreateBrush(string hex)
        {
            var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            b.Freeze();
            return b;
        }
    }
}
