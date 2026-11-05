using System;
using System.ComponentModel.DataAnnotations.Schema;
using QuanlyNhaSach.Models.Enums;

namespace QuanlyNhaSach.Models
{
    public partial class NGUOIDUNG
    {
        [NotMapped]
        public string UserCode => string.IsNullOrWhiteSpace(MaND) ? "ND0000" : MaND.Trim().ToUpperInvariant();

        
        public UserRole Role
        {
            get
            {
                string vt = Vaitro?.Trim() ?? string.Empty;
                if (vt.Contains("Quản lý", StringComparison.OrdinalIgnoreCase) ||
                    vt.Equals("Manager", StringComparison.OrdinalIgnoreCase) ||
                    vt.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                    MaND.StartsWith("QL", StringComparison.OrdinalIgnoreCase))
                {
                    return UserRole.Manager;
                }

                if (vt.Contains("Nhân viên", StringComparison.OrdinalIgnoreCase) ||
                    vt.Equals("Staff", StringComparison.OrdinalIgnoreCase) ||
                    MaND.StartsWith("NV", StringComparison.OrdinalIgnoreCase))
                {
                    return UserRole.Staff;
                }

                return UserRole.Customer;
            }
            set => Vaitro = value.ToVietnameseLabel();
        }

        [NotMapped]
        public bool IsActive
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Trangthai))
                {
                    return true;
                }

                string st = Trangthai.Trim();
                return !st.Contains("khóa", StringComparison.OrdinalIgnoreCase) &&
                       !st.Equals("Locked", StringComparison.OrdinalIgnoreCase) &&
                       !st.Equals("Inactive", StringComparison.OrdinalIgnoreCase);
            }
            set => Trangthai = value
                ? AccountStatus.Active.ToVietnameseLabel()
                : AccountStatus.Locked.ToVietnameseLabel();
        }

        [NotMapped]
        public string RoleDisplayName => Role.ToVietnameseLabel();

        [NotMapped]
        public string AccountStatusLabel => IsActive
            ? AccountStatus.Active.ToVietnameseLabel()
            : AccountStatus.Locked.ToVietnameseLabel();

        [NotMapped]
        public string ToggleLockActionText => IsActive ? "Khóa tài khoản" : "Mở khóa";

        [NotMapped]
        public string Initials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Hoten))
                {
                    return "ND";
                }

                string[] parts = Hoten.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1)
                {
                    return parts[0].Substring(0, 1).ToUpperInvariant();
                }

                return $"{parts[^2][0]}{parts[^1][0]}".ToUpperInvariant();
            }
        }
    }
}
