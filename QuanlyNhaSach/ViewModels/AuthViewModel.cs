using System;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanlyNhaSach.Services;

namespace QuanlyNhaSach.ViewModels
{
    public partial class AuthViewModel : ObservableObject
    {
        private static readonly Regex PhoneTenDigitsRegex = new(@"^\d{10}$", RegexOptions.Compiled);

        private readonly AuthService _authService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsRegisterMode))]
        private bool _isLoginMode = true;

        public bool IsRegisterMode => !IsLoginMode;

        // ==================== LOGIN PROPERTIES ====================
        [ObservableProperty]
        private string _loginUsername = string.Empty;

        [ObservableProperty]
        private string _loginPassword = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasLoginError))]
        private string _loginErrorMessage = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasLoginSuccess))]
        private string _loginSuccessMessage = string.Empty;

        public bool HasLoginError => !string.IsNullOrWhiteSpace(LoginErrorMessage);
        public bool HasLoginSuccess => !string.IsNullOrWhiteSpace(LoginSuccessMessage);

        // ==================== REGISTRATION PROPERTIES ====================
        [ObservableProperty]
        private string _registerHoTen = string.Empty;

        [ObservableProperty]
        private string _registerSDT = string.Empty;

        [ObservableProperty]
        private string _registerDiaChi = string.Empty;

        [ObservableProperty]
        private string _registerUsername = string.Empty;

        [ObservableProperty]
        private string _registerPassword = string.Empty;

        [ObservableProperty]
        private string _registerConfirmPassword = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasHoTenError))]
        private string _hoTenError = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasSdtError))]
        private string _sdtError = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasDiaChiError))]
        private string _diaChiError = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasUsernameError))]
        private string _usernameError = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPasswordError))]
        private string _passwordError = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasConfirmPasswordError))]
        private string _confirmPasswordError = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasRegisterError))]
        private string _registerErrorMessage = string.Empty;

        public bool HasHoTenError => !string.IsNullOrWhiteSpace(HoTenError);
        public bool HasSdtError => !string.IsNullOrWhiteSpace(SdtError);
        public bool HasDiaChiError => !string.IsNullOrWhiteSpace(DiaChiError);
        public bool HasUsernameError => !string.IsNullOrWhiteSpace(UsernameError);
        public bool HasPasswordError => !string.IsNullOrWhiteSpace(PasswordError);
        public bool HasConfirmPasswordError => !string.IsNullOrWhiteSpace(ConfirmPasswordError);
        public bool HasRegisterError => !string.IsNullOrWhiteSpace(RegisterErrorMessage);

        public AuthViewModel(
            AuthService authService,
            INavigationService navigationService,
            IDialogService dialogService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        }

        partial void OnLoginUsernameChanged(string value)
        {
            if (HasLoginError)
            {
                LoginErrorMessage = string.Empty;
            }
        }

        partial void OnLoginPasswordChanged(string value)
        {
            if (HasLoginError)
            {
                LoginErrorMessage = string.Empty;
            }
        }

        [RelayCommand]
        private void Login()
        {
            LoginErrorMessage = string.Empty;
            LoginSuccessMessage = string.Empty;

            var result = _authService.Login(LoginUsername, LoginPassword);
            if (!result.IsSuccess || result.Data == null)
            {
                LoginErrorMessage = result.Message;
                return;
            }

            // Clear password input state before navigating to role window
            LoginPassword = string.Empty;
            _navigationService.RouteAuthenticatedUser(result.Data);
        }

        [RelayCommand]
        private void FillDemoCustomer()
        {
            IsLoginMode = true;
            LoginErrorMessage = string.Empty;
            LoginSuccessMessage = "Đã điền tài khoản mẫu Khách hàng (Demo-Only). Nhấn 'Đăng nhập' để tiếp tục.";
            LoginUsername = "customer@example.com";
            LoginPassword = "Customer123!";
        }

        [RelayCommand]
        private void FillDemoStaff()
        {
            IsLoginMode = true;
            LoginErrorMessage = string.Empty;
            LoginSuccessMessage = "Đã điền tài khoản mẫu Nhân viên (Demo-Only). Nhấn 'Đăng nhập' để tiếp tục.";
            LoginUsername = "staff@example.com";
            LoginPassword = "Staff123!";
        }

        [RelayCommand]
        private void FillDemoManager()
        {
            IsLoginMode = true;
            LoginErrorMessage = string.Empty;
            LoginSuccessMessage = "Đã điền tài khoản mẫu Quản lý / Admin (Demo-Only). Nhấn 'Đăng nhập' để tiếp tục.";
            LoginUsername = "manager@example.com";
            LoginPassword = "Manager123!";
        }

        [RelayCommand]
        private void SwitchToRegister()
        {
            LoginErrorMessage = string.Empty;
            LoginSuccessMessage = string.Empty;
            ClearRegisterValidationErrors();
            IsLoginMode = false;
        }

        [RelayCommand]
        private void SwitchToLogin()
        {
            ClearRegisterValidationErrors();
            RegisterPassword = string.Empty;
            RegisterConfirmPassword = string.Empty;
            IsLoginMode = true;
        }

        [RelayCommand]
        private void Register()
        {
            ClearRegisterValidationErrors();

            bool isValid = ValidateRegistrationFields();
            if (!isValid)
            {
                RegisterErrorMessage = "Vui lòng kiểm tra và hoàn thiện các thông tin đăng ký hợp lệ.";
                return;
            }

            var result = _authService.RegisterCustomer(
                RegisterHoTen,
                RegisterSDT,
                RegisterDiaChi,
                RegisterUsername,
                RegisterPassword,
                RegisterConfirmPassword);

            if (!result.IsSuccess || result.Data == null)
            {
                RegisterErrorMessage = result.Message;
                return;
            }

            string registeredUsername = result.Data.Username;

            // Reset registration form
            RegisterHoTen = string.Empty;
            RegisterSDT = string.Empty;
            RegisterDiaChi = string.Empty;
            RegisterUsername = string.Empty;
            RegisterPassword = string.Empty;
            RegisterConfirmPassword = string.Empty;
            ClearRegisterValidationErrors();

            // Return to login mode with username pre-populated
            LoginUsername = registeredUsername;
            LoginPassword = string.Empty;
            LoginErrorMessage = string.Empty;
            LoginSuccessMessage = result.Message;
            IsLoginMode = true;

            _dialogService.ShowInformation("Đăng ký thành công", result.Message);
        }

        private bool ValidateRegistrationFields()
        {
            bool valid = true;

            if (string.IsNullOrWhiteSpace(RegisterHoTen))
            {
                HoTenError = "Vui lòng nhập họ và tên đầy đủ.";
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(RegisterSDT))
            {
                SdtError = "Vui lòng nhập số điện thoại liên hệ.";
                valid = false;
            }
            else if (!PhoneTenDigitsRegex.IsMatch(RegisterSDT.Trim()))
            {
                SdtError = "Số điện thoại phải gồm đúng 10 chữ số (0-9).";
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(RegisterDiaChi))
            {
                DiaChiError = "Vui lòng nhập địa chỉ nhận sách.";
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(RegisterUsername) || RegisterUsername.Trim().Length < 3)
            {
                UsernameError = "Tên đăng nhập / Email phải có ít nhất 3 ký tự.";
                valid = false;
            }
            else if (RegisterUsername.Trim().Contains(' '))
            {
                UsernameError = "Tên đăng nhập không được chứa khoảng trắng.";
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(RegisterPassword) || RegisterPassword.Length < 6)
            {
                PasswordError = "Mật khẩu phải có ít nhất 6 ký tự.";
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(RegisterConfirmPassword))
            {
                ConfirmPasswordError = "Vui lòng nhập lại mật khẩu xác nhận.";
                valid = false;
            }
            else if (!string.Equals(RegisterPassword, RegisterConfirmPassword, StringComparison.Ordinal))
            {
                ConfirmPasswordError = "Mật khẩu xác nhận không khớp với mật khẩu đã nhập.";
                valid = false;
            }

            return valid;
        }

        private void ClearRegisterValidationErrors()
        {
            HoTenError = string.Empty;
            SdtError = string.Empty;
            DiaChiError = string.Empty;
            UsernameError = string.Empty;
            PasswordError = string.Empty;
            ConfirmPasswordError = string.Empty;
            RegisterErrorMessage = string.Empty;
        }
    }
}
