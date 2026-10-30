using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

        private readonly MockDataStore _dataStore;

        public event EventHandler? UsersChanged;

        public UserManagementService(MockDataStore dataStore)
        {
            _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
        }

        public IReadOnlyList<User> GetUsersByRole(UserRole role)
        {
            return _dataStore.Users
                .Where(u => u.Role == role)
                .OrderBy(u => u.MaND)
                .ToList();
        }

        public IReadOnlyList<User> SearchAccounts(UserRole role, string? keyword, string? statusFilter = null)
        {
            IEnumerable<User> query = _dataStore.Users.Where(u => u.Role == role);

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
                if ((cleanId.StartsWith("NV", StringComparison.OrdinalIgnoreCase) ||
                     cleanId.StartsWith("KH", StringComparison.OrdinalIgnoreCase)) &&
                    cleanId.Length > 2)
                {
                    cleanId = cleanId.Substring(2).TrimStart('0');
                    if (string.IsNullOrEmpty(cleanId))
                    {
                        cleanId = "0";
                    }
                }

                query = query.Where(u =>
                    u.HoTen.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    u.Username.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    u.SDT.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    u.UserCode.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    u.MaND.ToString().Equals(cleanId, StringComparison.OrdinalIgnoreCase));
            }

            return query.OrderBy(u => u.MaND).ToList();
        }

        public OperationResult<User> AddStaffAccount(
            string? hoTen,
            string? sdt,
            string? diaChi,
            string? username,
            string? password,
            string? confirmPassword = null)
        {
            if (string.IsNullOrWhiteSpace(hoTen))
            {
                return OperationResult<User>.Failure("Vui lòng nhập họ và tên nhân viên.");
            }

            if (string.IsNullOrWhiteSpace(sdt) || !PhoneTenDigitsRegex.IsMatch(sdt.Trim()))
            {
                return OperationResult<User>.Failure("Số điện thoại nhân viên không hợp lệ. Vui lòng nhập đúng 10 chữ số (0-9).");
            }

            if (string.IsNullOrWhiteSpace(diaChi))
            {
                return OperationResult<User>.Failure("Vui lòng nhập địa chỉ liên hệ của nhân viên.");
            }

            if (string.IsNullOrWhiteSpace(username) || username.Trim().Length < 3)
            {
                return OperationResult<User>.Failure("Tên đăng nhập phải có tối thiểu 3 ký tự.");
            }

            string normalizedUsername = username.Trim();
            if (normalizedUsername.Contains(' '))
            {
                return OperationResult<User>.Failure("Tên đăng nhập không được chứa khoảng trắng.");
            }

            bool exists = _dataStore.Users.Any(u =>
                !string.IsNullOrWhiteSpace(u.Username) &&
                u.Username.Equals(normalizedUsername, StringComparison.OrdinalIgnoreCase));

            if (exists)
            {
                return OperationResult<User>.Failure(
                    $"Tên đăng nhập \"{normalizedUsername}\" đã tồn tại trong hệ thống. Vui lòng chọn tên đăng nhập khác.");
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                return OperationResult<User>.Failure("Mật khẩu khởi tạo phải có tối thiểu 6 ký tự.");
            }

            if (confirmPassword != null && !string.Equals(password, confirmPassword, StringComparison.Ordinal))
            {
                return OperationResult<User>.Failure("Mật khẩu xác nhận không khớp với mật khẩu khởi tạo.");
            }

            var newStaff = new Staff
            {
                MaND = _dataStore.GenerateNextUserId(),
                HoTen = hoTen.Trim(),
                SDT = sdt.Trim(),
                DiaChi = diaChi.Trim(),
                Username = normalizedUsername,
                Password = password,
                Role = UserRole.Staff, // Strictly enforced Staff role
                IsActive = true
            };

            _dataStore.Users.Add(newStaff);
            NotifyChanged();

            return OperationResult<User>.Success(
                newStaff,
                $"Đã thêm tài khoản nhân viên \"{newStaff.HoTen}\" ({newStaff.UserCode} — {newStaff.Username}) ở trạng thái Hoạt động.");
        }

        public OperationResult<User> ToggleAccountLock(int targetUserId, int? currentActorUserId = null)
        {
            var user = _dataStore.Users.FirstOrDefault(u => u.MaND == targetUserId);
            if (user == null)
            {
                return OperationResult<User>.Failure($"Không tìm thấy tài khoản người dùng #{targetUserId}.");
            }

            if (user.Role == UserRole.Manager)
            {
                return OperationResult<User>.Failure("Không được phép khóa hoặc thay đổi trạng thái tài khoản Quản lý (Manager) từ giao diện này.");
            }

            if (currentActorUserId.HasValue && user.MaND == currentActorUserId.Value)
            {
                return OperationResult<User>.Failure("Bạn không thể tự khóa tài khoản đang đăng nhập của chính mình.");
            }

            user.IsActive = !user.IsActive;
            NotifyChanged();

            string stateAction = user.IsActive ? "MỞ KHÓA" : "KHÓA";
            string loginEffect = user.IsActive
                ? "Tài khoản này đã có thể đăng nhập trở lại vào hệ thống."
                : "Tài khoản này sẽ bị từ chối đăng nhập ngay lập tức trên cổng xác thực AuthService.";

            return OperationResult<User>.Success(
                user,
                $"Đã {stateAction} tài khoản \"{user.HoTen}\" ({user.Username}).\n• Trạng thái hiện tại: {user.AccountStatusLabel}\n• {loginEffect}");
        }

        /// <summary>
        /// Demonstration-only password reset for a customer account.
        /// Generates a temporary mock password and updates the shared User record so it can log in immediately.
        /// </summary>
        public OperationResult<string> ResetCustomerPassword(int customerUserId)
        {
            var customer = _dataStore.Users.FirstOrDefault(u => u.MaND == customerUserId);
            if (customer == null)
            {
                return OperationResult<string>.Failure($"Không tìm thấy tài khoản khách hàng #{customerUserId}.");
            }

            if (customer.Role != UserRole.Customer)
            {
                return OperationResult<string>.Failure("Chức năng cấp lại mật khẩu tại màn hình này chỉ áp dụng cho tài khoản Khách hàng.");
            }

            int randomSuffix = Random.Shared.Next(100, 999);
            string temporaryPassword = $"BookNest@{customer.MaND:D2}{randomSuffix}";

            customer.Password = temporaryPassword;
            NotifyChanged();

            string msg =
                $"Đã cấp lại mật khẩu tạm thời (Mô phỏng) cho khách hàng:\n\n" +
                $"• Mã KH: {customer.UserCode}\n" +
                $"• Họ và tên: {customer.HoTen}\n" +
                $"• Tên đăng nhập: {customer.Username}\n" +
                $"• Mật khẩu tạm thời mới: {temporaryPassword}\n\n" +
                $"[Lưu ý mô phỏng]: Đây là cơ chế cấp lại mật khẩu minh họa trên bộ nhớ In-Memory (không gửi email/SMS thực tế). Mật khẩu tạm thời chỉ hiển thị một lần tại hộp thoại này và có hiệu lực đăng nhập ngay lập tức.";

            return OperationResult<string>.Success(temporaryPassword, msg);
        }

        private void NotifyChanged()
        {
            UsersChanged?.Invoke(this, EventArgs.Empty);
            _dataStore.NotifyDataChanged();
        }
    }
}
