using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using QuanlyNhaSach.Models;
using QuanlyNhaSach.Models.Enums;

namespace QuanlyNhaSach.Services
{
    public partial class AuthService : ObservableObject
    {
        private static readonly Regex PhoneTenDigitsRegex = new(@"^\d{10}$", RegexOptions.Compiled);

        private readonly List<NGUOIDUNG> _fallbackAccounts = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsAuthenticated))]
        [NotifyPropertyChangedFor(nameof(IsCustomerSession))]
        [NotifyPropertyChangedFor(nameof(IsStaffSession))]
        [NotifyPropertyChangedFor(nameof(IsManagerSession))]
        [NotifyPropertyChangedFor(nameof(IsGuestSession))]
        private NGUOIDUNG? _currentUser;

        public bool IsAuthenticated => CurrentUser != null && CurrentUser.IsActive;
        public bool IsCustomerSession => IsAuthenticated && CurrentUser!.Role == UserRole.Customer;
        public bool IsStaffSession => IsAuthenticated && CurrentUser!.Role == UserRole.Staff;
        public bool IsManagerSession => IsAuthenticated && CurrentUser!.Role == UserRole.Manager;
        public bool IsGuestSession => IsCustomerSession && string.Equals(CurrentUser?.Tendangnhap, "guest", StringComparison.OrdinalIgnoreCase);

        public event EventHandler<NGUOIDUNG?>? SessionChanged;

        public AuthService()
        {
            InitializeDefaultAccounts();
        }

        private void InitializeDefaultAccounts()
        {
            EnsureFallbackAccount(
                maND: "QL001",
                tendangnhap: "manager",
                email: "manager@example.com",
                password: "Manager123!",
                hoTen: "Quản lý Nhà sách",
                role: UserRole.Manager,
                sdt: "0900000001",
                diaChi: "Cổng Quản trị Nhà sách");

            EnsureFallbackAccount(
                maND: "NV001",
                tendangnhap: "staff",
                email: "staff@example.com",
                password: "Staff123!",
                hoTen: "Nhân viên Nhà sách",
                role: UserRole.Staff,
                sdt: "0900000002",
                diaChi: "Cổng Nhân viên Nhà sách");

            EnsureFallbackAccount(
                maND: "KH001",
                tendangnhap: "customer",
                email: "customer@example.com",
                password: "Customer123!",
                hoTen: "Nguyễn Minh Anh",
                role: UserRole.Customer,
                sdt: "0900000003",
                diaChi: "123 Nguyễn Văn Cừ, Quận 5, TP.HCM");

            TrySeedDefaultAccountsInDatabase();
        }

        private void EnsureFallbackAccount(
            string maND,
            string tendangnhap,
            string email,
            string password,
            string hoTen,
            UserRole role,
            string sdt,
            string diaChi)
        {
            if (_fallbackAccounts.Any(u => string.Equals(u.MaND, maND, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            _fallbackAccounts.Add(new NGUOIDUNG
            {
                MaND = maND,
                Tendangnhap = tendangnhap,
                Email = email,
                Matkhau = password,
                Hoten = hoTen,
                Vaitro = role.ToVietnameseLabel(),
                SDT = sdt,
                Diachi = diaChi,
                Trangthai = AccountStatus.Active.ToVietnameseLabel()
            });
        }

        private void TrySeedDefaultAccountsInDatabase()
        {
            try
            {
                using var db = new QuanlyNhaSachContext();
                bool changed = false;

                foreach (var seed in _fallbackAccounts)
                {
                    bool exists = db.NGUOIDUNGs.Any(u =>
                        u.MaND == seed.MaND ||
                        u.Tendangnhap == seed.Tendangnhap ||
                        (seed.Email != null && u.Email == seed.Email));

                    if (!exists)
                    {
                        db.NGUOIDUNGs.Add(new NGUOIDUNG
                        {
                            MaND = seed.MaND,
                            Tendangnhap = seed.Tendangnhap,
                            Email = seed.Email,
                            Matkhau = seed.Matkhau,
                            Hoten = seed.Hoten,
                            SDT = seed.SDT,
                            Diachi = seed.Diachi,
                            Vaitro = seed.Vaitro,
                            Trangthai = seed.Trangthai
                        });
                        changed = true;
                    }
                }

                if (changed)
                {
                    db.SaveChanges();
                }
            }
            catch
            {
                // Ignore if SQL Server is not yet reachable during startup
            }
        }

        public bool HasRole(UserRole role)
        {
            return IsAuthenticated && CurrentUser!.Role == role;
        }

        public OperationResult<NGUOIDUNG> Login(string? username, string? password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return OperationResult<NGUOIDUNG>.Failure("Vui lòng nhập tài khoản và mật khẩu.");
            }

            string normalizedUsername = username.Trim();
            NGUOIDUNG? matchedUser = null;

            try
            {
                using var db = new QuanlyNhaSachContext();
                matchedUser = db.NGUOIDUNGs
                    .AsNoTracking()
                    .FirstOrDefault(u =>
                        u.Tendangnhap.ToLower() == normalizedUsername.ToLower() ||
                        (u.Email != null && u.Email.ToLower() == normalizedUsername.ToLower()) ||
                        u.MaND.ToLower() == normalizedUsername.ToLower());
            }
            catch
            {
                // Fallback to in-memory default accounts if DB is unreachable
            }

            matchedUser ??= _fallbackAccounts.FirstOrDefault(u =>
                string.Equals(u.Tendangnhap, normalizedUsername, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(u.Email, normalizedUsername, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(u.Username, normalizedUsername, StringComparison.OrdinalIgnoreCase));

            if (matchedUser == null || !string.Equals(matchedUser.Matkhau, password, StringComparison.Ordinal))
            {
                return OperationResult<NGUOIDUNG>.Failure("Tên đăng nhập hoặc mật khẩu không chính xác.");
            }

            if (!matchedUser.IsActive)
            {
                return OperationResult<NGUOIDUNG>.Failure("Tài khoản này hiện đã bị khóa hoặc ngừng hoạt động.");
            }

            CurrentUser = matchedUser;
            SessionChanged?.Invoke(this, CurrentUser);
            return OperationResult<NGUOIDUNG>.Success(
                CurrentUser,
                $"Đăng nhập thành công! Xin chào {CurrentUser.HoTen} ({CurrentUser.RoleDisplayName}).");
        }

        public OperationResult<NGUOIDUNG> LoginAsGuest()
        {
            CurrentUser = new NGUOIDUNG
            {
                MaND = "KH000",
                Tendangnhap = "guest",
                Matkhau = string.Empty,
                Hoten = "Khách vãng lai (Guest)",
                SDT = string.Empty,
                Diachi = string.Empty,
                Vaitro = UserRole.Customer.ToVietnameseLabel(),
                Trangthai = AccountStatus.Active.ToVietnameseLabel()
            };

            SessionChanged?.Invoke(this, CurrentUser);
            return OperationResult<NGUOIDUNG>.Success(
                CurrentUser,
                "Đang truy cập Cổng Khách hàng với tư cách Khách (Guest).");
        }

        public OperationResult<NGUOIDUNG> RegisterCustomer(
            string? hoTen,
            string? sdt,
            string? diaChi,
            string? username,
            string? password,
            string? confirmPassword,
            UserRole? requestedRole = null)
        {
            if (string.IsNullOrWhiteSpace(hoTen))
            {
                return OperationResult<NGUOIDUNG>.Failure("Vui lòng nhập họ và tên đầy đủ.");
            }

            if (string.IsNullOrWhiteSpace(sdt) || !PhoneTenDigitsRegex.IsMatch(sdt.Trim()))
            {
                return OperationResult<NGUOIDUNG>.Failure("Số điện thoại phải gồm đúng 10 chữ số (0-9).");
            }

            if (string.IsNullOrWhiteSpace(diaChi))
            {
                return OperationResult<NGUOIDUNG>.Failure("Vui lòng nhập địa chỉ nhận sách.");
            }

            if (string.IsNullOrWhiteSpace(username) || username.Trim().Length < 3 || username.Trim().Contains(' '))
            {
                return OperationResult<NGUOIDUNG>.Failure("Tên đăng nhập / Email không hợp lệ (tối thiểu 3 ký tự, không khoảng trắng).");
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                return OperationResult<NGUOIDUNG>.Failure("Mật khẩu phải có ít nhất 6 ký tự.");
            }

            if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
            {
                return OperationResult<NGUOIDUNG>.Failure("Mật khẩu xác nhận không khớp.");
            }

            string normalizedName = hoTen.Trim();
            string normalizedPhone = sdt.Trim();
            string normalizedAddress = diaChi.Trim();
            string normalizedInputUsername = username.Trim();
            UserRole targetRole = requestedRole ?? UserRole.Customer;

            try
            {
                using var db = new QuanlyNhaSachContext();

                bool duplicate = db.NGUOIDUNGs
                    .AsNoTracking()
                    .Any(u =>
                        u.Tendangnhap.ToLower() == normalizedInputUsername.ToLower() ||
                        (u.Email != null && u.Email.ToLower() == normalizedInputUsername.ToLower()) ||
                        (u.SDT != null && u.SDT == normalizedPhone));

                if (duplicate)
                {
                    return OperationResult<NGUOIDUNG>.Failure(
                        "Tên đăng nhập, Email hoặc Số điện thoại này đã được đăng ký trước đó.");
                }

                string prefix = targetRole switch
                {
                    UserRole.Manager => "QL",
                    UserRole.Staff => "NV",
                    _ => "KH"
                };

                string newMaND = UserManagementService.GenerateNextUserId(db, prefix);
                string loginKey = UserManagementService.BuildUniqueShortLoginKey(db, normalizedInputUsername, newMaND);
                string? emailValue = normalizedInputUsername.Contains('@') || normalizedInputUsername.Length > 10
                    ? normalizedInputUsername
                    : null;

                var newUser = new NGUOIDUNG
                {
                    MaND = newMaND,
                    Tendangnhap = loginKey,
                    Email = emailValue,
                    Matkhau = password,
                    Hoten = normalizedName,
                    SDT = normalizedPhone,
                    Diachi = normalizedAddress,
                    Vaitro = targetRole.ToVietnameseLabel(),
                    Trangthai = AccountStatus.Active.ToVietnameseLabel()
                };

                db.NGUOIDUNGs.Add(newUser);
                db.SaveChanges();

                return OperationResult<NGUOIDUNG>.Success(
                    newUser,
                    $"Đăng ký tài khoản thành công! Vui lòng đăng nhập với tài khoản \"{normalizedInputUsername}\".");
            }
            catch (Exception ex)
            {
                return OperationResult<NGUOIDUNG>.Failure(
                    $"Không thể lưu tài khoản vào cơ sở dữ liệu: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        public void Logout()
        {
            if (CurrentUser != null)
            {
                CurrentUser = null;
                SessionChanged?.Invoke(this, null);
            }
        }
    }
}
