using System;
using System.Linq;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using QuanlyNhaSach.Models;
using QuanlyNhaSach.Models.Enums;

namespace QuanlyNhaSach.Services
{
    /// <summary>
    /// Demonstration authentication and session service backed by shared in-memory mock data.
    /// NOTE: This service is strictly for isolated prototype validation and does not provide
    /// production credential security (which requires password hashing and server-side identity).
    /// </summary>
    public partial class AuthService : ObservableObject
    {
        private static readonly Regex PhoneTenDigitsRegex = new(@"^\d{10}$", RegexOptions.Compiled);

        private readonly MockDataService _mockDataService;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsAuthenticated))]
        [NotifyPropertyChangedFor(nameof(IsCustomerSession))]
        [NotifyPropertyChangedFor(nameof(IsStaffSession))]
        [NotifyPropertyChangedFor(nameof(IsManagerSession))]
        private User? _currentUser;

        public bool IsAuthenticated => CurrentUser != null && CurrentUser.IsActive;
        public bool IsCustomerSession => IsAuthenticated && CurrentUser!.Role == UserRole.Customer;
        public bool IsStaffSession => IsAuthenticated && CurrentUser!.Role == UserRole.Staff;
        public bool IsManagerSession => IsAuthenticated && CurrentUser!.Role == UserRole.Manager;

        public event EventHandler<User?>? SessionChanged;

        public AuthService(MockDataService mockDataService)
        {
            _mockDataService = mockDataService ?? throw new ArgumentNullException(nameof(mockDataService));
        }

        public bool HasRole(UserRole role)
        {
            return IsAuthenticated && CurrentUser!.Role == role;
        }

        public OperationResult<User> Login(string? username, string? password)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return OperationResult<User>.Failure("Vui lòng nhập tên đăng nhập hoặc email.");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                return OperationResult<User>.Failure("Vui lòng nhập mật khẩu.");
            }

            string normalizedUsername = username.Trim();

            var matchedUser = _mockDataService.Users.FirstOrDefault(
                u => !string.IsNullOrWhiteSpace(u.Username) &&
                     u.Username.Equals(normalizedUsername, StringComparison.OrdinalIgnoreCase));

            if (matchedUser == null)
            {
                return OperationResult<User>.Failure("Tên đăng nhập hoặc mật khẩu không chính xác.");
            }

            if (!matchedUser.IsActive)
            {
                return OperationResult<User>.Failure(
                    "Tài khoản này hiện đã bị khóa hoặc ngừng hoạt động. Vui lòng liên hệ quản trị viên.");
            }

            if (!string.Equals(matchedUser.Password, password, StringComparison.Ordinal))
            {
                return OperationResult<User>.Failure("Tên đăng nhập hoặc mật khẩu không chính xác.");
            }

            CurrentUser = matchedUser;
            SessionChanged?.Invoke(this, CurrentUser);

            return OperationResult<User>.Success(
                matchedUser,
                $"Đăng nhập thành công! Xin chào {matchedUser.HoTen} ({matchedUser.RoleDisplayName}).");
        }

        public OperationResult<User> RegisterCustomer(
            string? hoTen,
            string? sdt,
            string? diaChi,
            string? username,
            string? password,
            string? confirmPassword,
            UserRole? requestedRole = null)
        {
            // Explicitly block any attempt to register a Staff account through public registration
            if (requestedRole.HasValue && requestedRole.Value != UserRole.Customer)
            {
                return OperationResult<User>.Failure(
                    "Chức năng đăng ký công khai chỉ dành cho tài khoản Khách hàng. Không được phép tự đăng ký quyền Nhân viên.");
            }

            if (string.IsNullOrWhiteSpace(hoTen))
            {
                return OperationResult<User>.Failure("Vui lòng nhập họ và tên khách hàng.");
            }

            if (string.IsNullOrWhiteSpace(sdt) || !PhoneTenDigitsRegex.IsMatch(sdt.Trim()))
            {
                return OperationResult<User>.Failure("Số điện thoại không hợp lệ. Vui lòng nhập đúng 10 chữ số (0-9).");
            }

            if (string.IsNullOrWhiteSpace(diaChi))
            {
                return OperationResult<User>.Failure("Vui lòng nhập địa chỉ liên hệ / giao hàng.");
            }

            if (string.IsNullOrWhiteSpace(username) || username.Trim().Length < 3)
            {
                return OperationResult<User>.Failure("Tên đăng nhập (hoặc email) phải có ít nhất 3 ký tự.");
            }

            string normalizedUsername = username.Trim();
            if (normalizedUsername.Contains(' '))
            {
                return OperationResult<User>.Failure("Tên đăng nhập không được chứa khoảng trắng.");
            }

            bool usernameExists = _mockDataService.Users.Any(
                u => !string.IsNullOrWhiteSpace(u.Username) &&
                     u.Username.Equals(normalizedUsername, StringComparison.OrdinalIgnoreCase));

            if (usernameExists)
            {
                return OperationResult<User>.Failure(
                    $"Tên đăng nhập \"{normalizedUsername}\" đã tồn tại trong hệ thống. Vui lòng chọn tên đăng nhập khác.");
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                return OperationResult<User>.Failure("Mật khẩu phải có độ dài tối thiểu 6 ký tự.");
            }

            if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
            {
                return OperationResult<User>.Failure("Mật khẩu xác nhận không khớp với mật khẩu đã nhập.");
            }

            var newCustomer = new User
            {
                MaND = _mockDataService.GenerateNextUserId(),
                HoTen = hoTen.Trim(),
                SDT = sdt.Trim(),
                DiaChi = diaChi.Trim(),
                Username = normalizedUsername,
                Password = password,
                Role = UserRole.Customer, // Enforced Customer-only role
                IsActive = true
            };

            _mockDataService.Users.Add(newCustomer);
            _mockDataService.NotifyDataChanged();

            return OperationResult<User>.Success(
                newCustomer,
                $"Đăng ký tài khoản khách hàng \"{newCustomer.Username}\" thành công! Bạn có thể đăng nhập ngay.");
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
