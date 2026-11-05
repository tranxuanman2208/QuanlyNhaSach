using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using QuanlyNhaSach.Models;
using QuanlyNhaSach.Models.Enums;

namespace QuanlyNhaSach.Services
{
    public class UserManagementService
    {
        public const string AllStatusFilter = "Tất cả";
        public const string ActiveStatusFilter = "Hoạt động";
        public const string LockedStatusFilter = "Đã khóa";

        private static readonly Regex PhoneTenDigitsRegex = new(@"^\d{10}$", RegexOptions.Compiled);

        private readonly object _cacheLock = new();
        private List<NGUOIDUNG>? _usersCache;

        public event EventHandler? UsersChanged;

        public UserManagementService()
        {
        }

        public void InvalidateCache()
        {
            lock (_cacheLock)
            {
                _usersCache = null;
            }
        }

        private IReadOnlyList<NGUOIDUNG> GetAllUsersCached()
        {
            lock (_cacheLock)
            {
                if (_usersCache != null)
                {
                    return _usersCache;
                }

                try
                {
                    using var db = new QuanlyNhaSachContext();
                    _usersCache = db.NGUOIDUNGs
                        .AsNoTracking()
                        .OrderBy(u => u.MaND)
                        .ToList();
                    return _usersCache;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Lỗi khi tải danh sách người dùng: {ex.Message}");
                    return Array.Empty<NGUOIDUNG>();
                }
            }
        }

        public IReadOnlyList<NGUOIDUNG> GetUsersByRole(UserRole role)
        {
            return GetAllUsersCached()
                .Where(u => u.Role == role)
                .ToList();
        }

        public IReadOnlyList<NGUOIDUNG> SearchAccounts(UserRole role, string? keyword, string? statusFilter = null)
        {
            IEnumerable<NGUOIDUNG> query = GetUsersByRole(role);

            if (!string.IsNullOrWhiteSpace(statusFilter) &&
                !statusFilter.Equals(AllStatusFilter, StringComparison.OrdinalIgnoreCase))
            {
                if (statusFilter.Equals(ActiveStatusFilter, StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(u => u.IsActive);
                }
                else if (statusFilter.Equals(LockedStatusFilter, StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(u => !u.IsActive);
                }
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string kw = keyword.Trim();
                string cleanId = kw.TrimStart('#');

                query = query.Where(u =>
                    (u.Hoten?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (u.Tendangnhap?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (u.Email?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (u.SDT?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    u.UserCode.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    u.MaND.Contains(cleanId, StringComparison.OrdinalIgnoreCase));
            }

            return query.OrderBy(u => u.MaND).ToList();
        }

        public OperationResult<NGUOIDUNG> AddStaffAccount(
            string? hoTen,
            string? sdt,
            string? email,
            string? diaChi,
            string? username,
            string? password,
            string? confirmPassword = null)
        {
            if (string.IsNullOrWhiteSpace(hoTen))
                return OperationResult<NGUOIDUNG>.Failure("Vui lòng nhập họ và tên nhân viên.");
            if (hoTen.Trim().Length > 100)
                return OperationResult<NGUOIDUNG>.Failure("Họ và tên nhân viên không được vượt quá 100 ký tự.");
            if (string.IsNullOrWhiteSpace(sdt) || !PhoneTenDigitsRegex.IsMatch(sdt.Trim()))
                return OperationResult<NGUOIDUNG>.Failure("Số điện thoại nhân viên không hợp lệ. Vui lòng nhập đúng 10 chữ số (0-9).");
            if (string.IsNullOrWhiteSpace(email) || !email.EndsWith("@gmail.com") || email.Length < 10)
                return OperationResult<NGUOIDUNG>.Failure("Vui lòng nhập email hợp lệ!");
            if (string.IsNullOrWhiteSpace(diaChi))
                return OperationResult<NGUOIDUNG>.Failure("Vui lòng nhập địa chỉ liên hệ của nhân viên.");
            if (diaChi.Trim().Length > 100)
                return OperationResult<NGUOIDUNG>.Failure("Địa chỉ nhân viên không được vượt quá 100 ký tự.");
            if (string.IsNullOrWhiteSpace(username) || username.Trim().Length < 3)
                return OperationResult<NGUOIDUNG>.Failure("Tên đăng nhập phải có ít nhất 3 ký tự.");
            if (username.Trim().Contains(' '))
                return OperationResult<NGUOIDUNG>.Failure("Tên đăng nhập không được chứa khoảng trắng.");
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                return OperationResult<NGUOIDUNG>.Failure("Mật khẩu khởi tạo phải có ít nhất 6 ký tự.");
            if (confirmPassword != null && !string.Equals(password, confirmPassword, StringComparison.Ordinal))
                return OperationResult<NGUOIDUNG>.Failure("Mật khẩu xác nhận không khớp với mật khẩu khởi tạo.");

            string normalizedName = hoTen.Trim();
            string normalizedPhone = sdt.Trim();
            string normalizeEmail = email.Trim();
            string normalizedAddress = diaChi.Trim();
            string normalizedInputUsername = username.Trim();

            try
            {
                using var db = new QuanlyNhaSachContext();

                bool duplicateExists = db.NGUOIDUNGs
                    .AsNoTracking()
                    .Any(u =>
                        u.Tendangnhap.ToLower() == normalizedInputUsername.ToLower() ||
                        (u.Email != null && u.Email.ToLower() == normalizedInputUsername.ToLower()) ||
                        (u.SDT != null && u.SDT == normalizedPhone));

                if (duplicateExists)
                {
                    return OperationResult<NGUOIDUNG>.Failure(
                        "Tên đăng nhập, Email hoặc Số điện thoại này đã được sử dụng bởi một tài khoản khác.");
                }

                string newMaND = GenerateNextUserId(db, "NV");
                string loginKey = BuildUniqueShortLoginKey(db, normalizedInputUsername, newMaND);

                var newStaff = new NGUOIDUNG
                {
                    MaND = newMaND,
                    Tendangnhap = loginKey,
                    Matkhau = BCrypt.Net.BCrypt.HashPassword(password),
                    Hoten = normalizedName,
                    SDT = normalizedPhone,
                    Email = normalizeEmail,
                    Diachi = normalizedAddress,
                    Vaitro = UserRole.Staff.ToVietnameseLabel(),
                    Trangthai = AccountStatus.Active.ToVietnameseLabel()
                };

                db.NGUOIDUNGs.Add(newStaff);
                db.SaveChanges();

                InvalidateCache();
                NotifyChanged();
                return OperationResult<NGUOIDUNG>.Success(
                    newStaff,
                    $"Đã tạo tài khoản nhân viên \"{newStaff.Hoten}\" ({newStaff.UserCode} — {newStaff.Tendangnhap}) thành công.");
            }
            catch (Exception ex)
            {
                return OperationResult<NGUOIDUNG>.Failure(
                    $"Không thể tạo tài khoản: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        public OperationResult<NGUOIDUNG> ToggleAccountLock(
            string? targetMaND,
            string? currentActorMaND = null)
        {
            if (string.IsNullOrWhiteSpace(targetMaND))
            {
                return OperationResult<NGUOIDUNG>.Failure("Mã người dùng không hợp lệ.");
            }

            string targetKey = targetMaND.Trim();
            if (!string.IsNullOrWhiteSpace(currentActorMaND) &&
                string.Equals(targetKey, currentActorMaND.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return OperationResult<NGUOIDUNG>.Failure("Bạn không thể khóa tài khoản Quản lý đang đăng nhập.");
            }

            try
            {
                using var db = new QuanlyNhaSachContext();
                var user = db.NGUOIDUNGs.FirstOrDefault(u => u.MaND == targetKey);
                if (user == null)
                {
                    return OperationResult<NGUOIDUNG>.Failure($"Không tìm thấy tài khoản trong hệ thống.");
                }

                if (user.Role == UserRole.Manager && user.IsActive)
                {
                    int activeManagersCount = GetAllUsersCached()
                        .Count(u => u.Role == UserRole.Manager && u.IsActive);

                    if (activeManagersCount <= 1)
                    {
                        return OperationResult<NGUOIDUNG>.Failure(
                            "Không thể khóa tài khoản Quản lý đang hoạt động trong hệ thống.");
                    }
                }

                bool nextActiveState = !user.IsActive;
                user.IsActive = nextActiveState;

                db.SaveChanges();
                InvalidateCache();
                NotifyChanged();

                string stateText = nextActiveState ? "Mở khóa (Hoạt động)" : "Khóa tài khoản";
                return OperationResult<NGUOIDUNG>.Success(
                    user,
                    $"Đã cập nhật trạng thái tài khoản \"{user.Hoten}\" ({user.UserCode}) thành: {stateText}.");
            }
            catch (Exception ex)
            {
                return OperationResult<NGUOIDUNG>.Failure(
                    $"Không thể cập nhật trạng thái tài khoản: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        public OperationResult<string> ResetCustomerPassword(string? customerMaND)
        {
            if (string.IsNullOrWhiteSpace(customerMaND))
            {
                return OperationResult<string>.Failure("Mã khách hàng không hợp lệ.");
            }

            string key = customerMaND.Trim();

            try
            {
                using var db = new QuanlyNhaSachContext();
                var customer = db.NGUOIDUNGs.FirstOrDefault(u => u.MaND == key);
                if (customer == null)
                {
                    return OperationResult<string>.Failure($"Không tìm thấy khách hàng #{key} trong cơ sở dữ liệu.");
                }

                string tempPassword = $"NS@{Random.Shared.Next(1000, 9999)}";
                customer.Matkhau = BCrypt.Net.BCrypt.HashPassword(tempPassword);

                db.SaveChanges();
                InvalidateCache();
                NotifyChanged();

                return OperationResult<string>.Success(
                    tempPassword,
                    $"Đã cấp lại mật khẩu cho khách hàng \"{customer.Hoten}\" ({customer.UserCode}).\n\n" +
                    $"• Tên đăng nhập: {customer.Tendangnhap}\n" +
                    $"• Mật khẩu mới: {tempPassword}\n\n");
            }
            catch (Exception ex)
            {
                return OperationResult<string>.Failure(
                    $"Không thể cấp lại mật khẩu: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        internal static string GenerateNextUserId(QuanlyNhaSachContext db, string prefix)
        {
            var existingIds = db.NGUOIDUNGs
                .AsNoTracking()
                .Where(u => u.MaND.StartsWith(prefix))
                .Select(u => u.MaND)
                .ToList();

            int maxNum = 0;
            foreach (var id in existingIds)
            {
                if (string.IsNullOrWhiteSpace(id))
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
            string candidate = $"{prefix.ToUpperInvariant()}{next:D3}";
            while (existingIds.Any(x => string.Equals(x?.Trim(), candidate, StringComparison.OrdinalIgnoreCase)))
            {
                next++;
                candidate = $"{prefix.ToUpperInvariant()}{next:D3}";
            }

            return candidate;
        }

        internal static string BuildUniqueShortLoginKey(
            QuanlyNhaSachContext db,
            string rawUsername,
            string fallbackMaND)
        {
            string baseKey = rawUsername.Trim();
            if (baseKey.Contains('@'))
            {
                baseKey = baseKey.Split('@')[0];
            }

            if (baseKey.Length > 10)
            {
                baseKey = baseKey.Substring(0, 10);
            }

            if (string.IsNullOrWhiteSpace(baseKey))
            {
                baseKey = fallbackMaND;
            }

            bool exists = db.NGUOIDUNGs
                .AsNoTracking()
                .Any(u => u.Tendangnhap == baseKey);

            if (!exists)
            {
                return baseKey;
            }

            return fallbackMaND.Length <= 10 ? fallbackMaND : fallbackMaND.Substring(0, 10);
        }

        private void NotifyChanged()
        {
            UsersChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}