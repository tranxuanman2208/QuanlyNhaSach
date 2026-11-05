using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanlyNhaSach.Models
{
    public partial class NHACUNGCAP
    {
        [NotMapped]
        public string SupplierCode
        {
            get
            {
                if (string.IsNullOrWhiteSpace(MaNCC))
                {
                    return "NCC0000";
                }

                string trimmed = MaNCC.Trim();
                if (trimmed.StartsWith("NCC", StringComparison.OrdinalIgnoreCase))
                {
                    return trimmed.ToUpperInvariant();
                }

                if (int.TryParse(trimmed, out int numericId))
                {
                    return $"NCC{numericId:D4}";
                }

                return trimmed;
            }
        }

        [NotMapped]
        public string DiaChi
        {
            get => Diachi ?? string.Empty;
            set => Diachi = value;
        }

        [NotMapped]
        public bool IsActive { get; set; } = true;

        [NotMapped]
        public string StatusLabel => IsActive ? "Đang hợp tác" : "Ngừng hợp tác";

        [NotMapped]
        public string StatusText => StatusLabel;

        [NotMapped]
        public string ToggleStatusActionText => IsActive ? "Ngừng HT" : "Kích hoạt";

        [NotMapped]
        public string DisplayName => $"{TenNCC ?? "Chưa đặt tên"} ({SupplierCode})";
    }
}
