using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using QuanlyNhaSach.Models;
using QuanlyNhaSach.Models.Enums;
using Bcrypytpass = BCrypt.Net.BCrypt;

namespace QuanlyNhaSach.Services
{
    public partial class AuthService : ObservableObject
    {
        private static readonly Regex PhoneTenDigitsRegex = new(@"^\d{10}$", RegexOptions.Compiled);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsAuthenticated))]
        [NotifyPropertyChangedFor(nameof(IsCustomerSession))]
        [NotifyPropertyChangedFor(nameof(IsStaffSession))]
        [NotifyPropertyChangedFor(nameof(IsManagerSession))]
        private NGUOIDUNG? _currentUser;

        public bool IsAuthenticated => CurrentUser != null && CurrentUser.IsActive;
        public bool IsCustomerSession => IsAuthenticated && CurrentUser!.Role == UserRole.Customer;
        public bool IsStaffSession => IsAuthenticated && CurrentUser!.Role == UserRole.Staff;
        public bool IsManagerSession => IsAuthenticated && CurrentUser!.Role == UserRole.Manager;

        public event EventHandler<NGUOIDUNG?>? SessionChanged;

        public AuthService()
        {
        }

        public bool HasRole(UserRole role)
        {
            return IsAuthenticated && CurrentUser!.Role == role;
        }

        public bool IsValidGmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            // Xóa khoảng trắng 2 đầu và chuyển về chữ thường để kiểm tra
            string cleanEmail = email.Trim().ToLower();

            // Phải kết thúc bằng @gmail.com và độ dài phải lớn hơn 10 (ví dụ: a@gmail.com)
            return cleanEmail.EndsWith("@gmail.com") && cleanEmail.Length > 10;
        }

        public async Task<OperationResult<NGUOIDUNG>> LoginAsync(string? username, string? password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return OperationResult<NGUOIDUNG>.Failure("Vui lòng nhập tên đăng nhập / email và mật khẩu.");
            }

            string norusername = username.Trim().ToLower();
            NGUOIDUNG? matchedUser = null;

            try
            {
                using var db = new QuanlyNhaSachContext();
                matchedUser = await db.NGUOIDUNGs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Tendangnhap.ToLower() == norusername || (u.Email != null && u.Email.ToLower() == norusername));
            }
            catch (Exception ex)
            {
                return OperationResult<NGUOIDUNG>.Failure($"Không thể kết nối đến cơ sở dữ liệu: {ex.Message}");
            }

            if (matchedUser == null)
            {
                return OperationResult<NGUOIDUNG>.Failure("Không tìm thấy tài khoản. Vui lòng kiểm tra lại tên đăng nhập hoặc email!");
            }

            bool isPasswordValid = false;
            try
            {
                isPasswordValid = await Task.Run(() => Bcrypytpass.Verify(password, matchedUser.Matkhau));
            }
            catch
            {
                // Hỗ trợ đối sánh trường hợp mật khẩu lưu dạng thô (plaintext) trong database cũ
                isPasswordValid = string.Equals(matchedUser.Matkhau, password, StringComparison.Ordinal);
            }

            if (!isPasswordValid)
            {
                return OperationResult<NGUOIDUNG>.Failure("Mật khẩu không chính xác. Vui lòng kiểm tra lại!");
            }

            if (!matchedUser.IsActive)
            {
                return OperationResult<NGUOIDUNG>.Failure("Tài khoản này hiện đã bị khóa hoặc ngừng hoạt động.");
            }

            CurrentUser = matchedUser;
            SessionChanged?.Invoke(this, CurrentUser);
            return OperationResult<NGUOIDUNG>.Success(
                CurrentUser,
                $"Đăng nhập thành công! Xin chào {CurrentUser.Hoten} ({CurrentUser.RoleDisplayName}).");
        }

        /// <summary>
        /// Kiểm tra dữ liệu đăng ký người dùng (định dạng + trùng lặp trong CSDL) mà KHÔNG lưu.
        /// Dùng trước khi gửi mã xác thực email và trước khi tạo tài khoản.
        /// </summary>
        public OperationResult ValidateCustomerRegistration(
            string? hoTen,
            string? sdt,
            string? email,
            string? diaChi,
            string? username,
            string? password,
            string? confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(hoTen))
            {
                return OperationResult.Failure("Vui lòng nhập họ và tên đầy đủ.");
            }

            if (string.IsNullOrWhiteSpace(sdt) || !PhoneTenDigitsRegex.IsMatch(sdt.Trim()))
            {
                return OperationResult.Failure("Số điện thoại phải gồm đúng 10 chữ số (0-9).");
            }

            if (string.IsNullOrWhiteSpace(diaChi))
            {
                return OperationResult.Failure("Vui lòng nhập địa chỉ.");
            }

            if (!IsValidGmail(email))
            {
                return OperationResult.Failure("Vui lòng nhập email hợp lệ (định dạng @gmail.com) để nhận mã xác thực.");
            }

            if (string.IsNullOrWhiteSpace(username) || username.Trim().Length < 3 || username.Trim().Contains(' '))
            {
                return OperationResult.Failure("Tên đăng nhập không hợp lệ (tối thiểu 3 ký tự, không khoảng trắng).");
            }

            if (username.Trim().Length > 10)
            {
                return OperationResult.Failure("Tên đăng nhập không được vượt quá 10 ký tự.");
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                return OperationResult.Failure("Mật khẩu phải có ít nhất 6 ký tự.");
            }

            if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
            {
                return OperationResult.Failure("Mật khẩu xác nhận không khớp.");
            }

            return UserManagementService.CheckAccountAvailability(username, email, sdt,
                "Tên đăng nhập, Email hoặc Số điện thoại này đã được đăng ký trước đó.");
        }

        public OperationResult<NGUOIDUNG> RegisterCustomer(
            string? hoTen,
            string? sdt,
            string? email,
            string? diaChi,
            string? username,
            string? password,
            string? confirmPassword,
            UserRole role = UserRole.Customer)
        {
            var validation = ValidateCustomerRegistration(hoTen, sdt, email, diaChi, username, password, confirmPassword);
            if (!validation.IsSuccess)
            {
                return OperationResult<NGUOIDUNG>.Failure(validation.Message);
            }

            string normalizedName = hoTen!.Trim();
            string normalizedPhone = sdt!.Trim();
            string normalizedAddress = diaChi!.Trim();
            string normalizedInputUsername = username!.Trim();
            string normalizedInputEmail = email!.Trim();

            try
            {
                using var db = new QuanlyNhaSachContext();

                string prefix = role switch
                {
                    UserRole.Manager => "QL",
                    UserRole.Staff => "NV",
                    _ => "KH"
                };

                string newMaND = UserManagementService.GenerateNextUserId(db, prefix);
                string passwordBcrypt = Bcrypytpass.HashPassword(password);
                var newUser = new NGUOIDUNG
                {
                    MaND = newMaND,
                    Tendangnhap = normalizedInputUsername,
                    Email = normalizedInputEmail,
                    Matkhau = passwordBcrypt,
                    Hoten = normalizedName,
                    SDT = normalizedPhone,
                    Diachi = normalizedAddress,
                    Vaitro = role.ToVietnameseLabel(),
                    Trangthai = $"Hoạt động ({DateTime.Today:dd/MM/yyyy})"
                };

                db.NGUOIDUNGs.Add(newUser);
                db.SaveChanges();

                return OperationResult<NGUOIDUNG>.Success(
                    newUser,
                    $"Đăng ký tài khoản {role.ToVietnameseLabel()} thành công! Vui lòng đăng nhập với tài khoản \"{normalizedInputUsername}\".");
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
