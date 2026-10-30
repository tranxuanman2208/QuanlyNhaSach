using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using QuanlyNhaSach.Models.Enums;

namespace QuanlyNhaSach.Models
{
    /// <summary>
    /// Unified User domain model for Customer, Staff, and Manager roles.
    /// NOTE (Prototype Security Scope): Password is stored as in-memory demonstration test data only
    /// for this isolated database-free prototype. Production systems must use secure password hashing
    /// and a server-side identity provider.
    /// </summary>
    public partial class User : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(UserCode))]
        private int _maND;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplaySummary))]
        [NotifyPropertyChangedFor(nameof(Initials))]
        private string _hoTen = string.Empty;

        private string _sdt = string.Empty;
        public string SDT
        {
            get => _sdt;
            set => SetProperty(ref _sdt, value);
        }

        [ObservableProperty]
        private string _diaChi = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplaySummary))]
        private string _username = string.Empty;

        /// <summary>
        /// Demo-only plaintext password for in-memory mock authentication during the current session.
        /// Never use plaintext passwords in production systems.
        /// </summary>
        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RoleDisplayName))]
        [NotifyPropertyChangedFor(nameof(IsStaff))]
        [NotifyPropertyChangedFor(nameof(IsCustomer))]
        [NotifyPropertyChangedFor(nameof(IsManager))]
        [NotifyPropertyChangedFor(nameof(DisplaySummary))]
        [NotifyPropertyChangedFor(nameof(UserCode))]
        private UserRole _role = UserRole.Customer;

        private bool _isActive = true;
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (SetProperty(ref _isActive, value))
                {
                    OnPropertyChanged(nameof(AccountStatus));
                    OnPropertyChanged(nameof(AccountStatusLabel));
                    OnPropertyChanged(nameof(IsLocked));
                    OnPropertyChanged(nameof(ToggleLockActionText));
                }
            }
        }

        public AccountStatus AccountStatus
        {
            get => _isActive ? AccountStatus.Active : AccountStatus.Locked;
            set => IsActive = (value == AccountStatus.Active);
        }

        public bool IsLocked => !_isActive;
        public string AccountStatusLabel => AccountStatus.ToVietnameseLabel();
        public string ToggleLockActionText => _isActive ? "Khóa tài khoản" : "Mở khóa tài khoản";

        public bool IsStaff => Role == UserRole.Staff;
        public bool IsCustomer => Role == UserRole.Customer;
        public bool IsManager => Role == UserRole.Manager;
        public string RoleDisplayName => Role.ToVietnameseLabel();
        public string DisplaySummary => $"{HoTen} ({RoleDisplayName})";

        public string UserCode => Role switch
        {
            UserRole.Manager => $"QL{MaND:D3}",
            UserRole.Staff => $"NV{MaND:D3}",
            _ => $"KH{MaND:D3}"
        };

        public string Initials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(HoTen))
                {
                    return "ND";
                }

                var parts = HoTen.Trim()
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1)
                {
                    return parts[0].Substring(0, 1).ToUpperInvariant();
                }

                return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
            }
        }
    }
}
