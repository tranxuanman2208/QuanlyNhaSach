using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanlyNhaSach.Models;
using QuanlyNhaSach.Models.Enums;
using QuanlyNhaSach.Services;

namespace QuanlyNhaSach.ViewModels
{
    public partial class AuthViewModel : ObservableObject
    {
        private const string UsernameTooLongInlineError = "Tên đăng nhập không được vượt quá 10 ký tự.";
        private const string UsernameTooLongBannerError = "Tên đăng nhập dùng để đăng ký không được vượt quá 10 ký tự.";

        private static readonly Regex PhoneTenDigitsRegex = new(@"^\d{10}$", RegexOptions.Compiled);

        private readonly AuthService _authService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
        private readonly EmailService _emailService;

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
        private UserRole _selectedRole = UserRole.Customer;

        public string RegisterButtonText => "Hoàn tất đăng ký";

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
        private string _registerEmail = string.Empty;

        [ObservableProperty]
        private string _registerVerificationCode = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasVerificationCodeError))]
        private string _verificationCodeError = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasVerificationInfo))]
        private string _verificationInfoMessage = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SendCodeButtonText))]
        private bool _isSendingCode;

        public bool HasVerificationCodeError => !string.IsNullOrWhiteSpace(VerificationCodeError);
        public bool HasVerificationInfo => !string.IsNullOrWhiteSpace(VerificationInfoMessage);
        public string SendCodeButtonText => IsSendingCode ? "Đang gửi..." : "Gửi mã";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasHoTenError))]
        private string _hoTenError = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasSdtError))]
        private string _sdtError = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasEmailError))]
        private string _emailError = string.Empty;

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
        public bool HasEmailError => !string.IsNullOrWhiteSpace(EmailError);
        public bool HasDiaChiError => !string.IsNullOrWhiteSpace(DiaChiError);
        public bool HasUsernameError => !string.IsNullOrWhiteSpace(UsernameError);
        public bool HasPasswordError => !string.IsNullOrWhiteSpace(PasswordError);
        public bool HasConfirmPasswordError => !string.IsNullOrWhiteSpace(ConfirmPasswordError);
        public bool HasRegisterError => !string.IsNullOrWhiteSpace(RegisterErrorMessage);

        public AuthViewModel(
            AuthService authService,
            INavigationService navigationService,
            IDialogService dialogService,
            EmailService? emailService = null)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _emailService = emailService ?? new EmailService();
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

        partial void OnRegisterUsernameChanged(string value)
        {
            string trimmed = value?.Trim() ?? string.Empty;

            if (trimmed.Length > 10)
            {
                UsernameError = UsernameTooLongInlineError;
                RegisterErrorMessage = UsernameTooLongBannerError;
            }
            else if (trimmed.Contains(' '))
            {
                UsernameError = "Tên đăng nhập không được chứa khoảng trắng.";
                if (RegisterErrorMessage == UsernameTooLongBannerError)
                {
                    RegisterErrorMessage = string.Empty;
                }
            }
            else
            {
                UsernameError = string.Empty;
                if (RegisterErrorMessage == UsernameTooLongBannerError)
                {
                    RegisterErrorMessage = string.Empty;
                }
            }
        }

        [RelayCommand]
        private async Task Login()
        {
            LoginErrorMessage = string.Empty;
            LoginSuccessMessage = string.Empty;

            var result = await _authService.LoginAsync(LoginUsername, LoginPassword);
            if (!result.IsSuccess || result.Data == null)
            {
                LoginErrorMessage = result.Message;
                return;
            }

            LoginPassword = string.Empty;
            _navigationService.RouteAuthenticatedUser(result.Data);
        }

        [RelayCommand]
        private void SwitchToRegister()
        {
            LoginErrorMessage = string.Empty;
            LoginSuccessMessage = string.Empty;
            ClearRegisterValidationErrors();
            SelectedRole = UserRole.Customer;
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
        private async Task SendVerificationCode()
        {
            ClearRegisterValidationErrors();
            VerificationInfoMessage = string.Empty;

            if (!ValidateRegistrationFields())
            {
                if (string.IsNullOrWhiteSpace(RegisterErrorMessage))
                {
                    RegisterErrorMessage = "Vui lòng hoàn thiện thông tin đăng ký hợp lệ trước khi gửi mã xác thực.";
                }
                return;
            }

            var validation = _authService.ValidateCustomerRegistration(
                RegisterHoTen, RegisterSDT, RegisterEmail, RegisterDiaChi,
                RegisterUsername, RegisterPassword, RegisterConfirmPassword);
            if (!validation.IsSuccess)
            {
                RegisterErrorMessage = validation.Message;
                return;
            }

            IsSendingCode = true;
            try
            {
                var emailResult = await _emailService.SendVerificationCodeAsync(
                    RegisterEmail, RegisterHoTen, UserRole.Customer, RegisterUsername);

                if (!emailResult.IsSuccess)
                {
                    RegisterErrorMessage = emailResult.Message;
                    return;
                }

                VerificationInfoMessage = emailResult.Message + " Nhập mã vào ô bên dưới để hoàn tất đăng ký.";
            }
            finally
            {
                IsSendingCode = false;
            }
        }

        [RelayCommand]
        private void Register()
        {
            ClearRegisterValidationErrors();

            bool isValid = ValidateRegistrationFields();
            if (!isValid)
            {
                if (string.IsNullOrWhiteSpace(RegisterErrorMessage))
                {
                    RegisterErrorMessage = "Vui lòng kiểm tra và hoàn thiện các thông tin đăng ký hợp lệ.";
                }
                return;
            }

            if (string.IsNullOrWhiteSpace(RegisterVerificationCode))
            {
                VerificationCodeError = "Vui lòng nhập mã xác thực đã gửi đến email (bấm \"Gửi mã\" nếu chưa nhận).";
                return;
            }

            var verifyResult = _emailService.VerifyCode(RegisterEmail, RegisterVerificationCode, consumeOnSuccess: false);
            if (!verifyResult.IsSuccess)
            {
                VerificationCodeError = verifyResult.Message;
                return;
            }

            var result = _authService.RegisterCustomer(
                RegisterHoTen,
                RegisterSDT,
                RegisterEmail,
                RegisterDiaChi,
                RegisterUsername,
                RegisterPassword,
                RegisterConfirmPassword,
                UserRole.Customer);

            if (!result.IsSuccess || result.Data == null)
            {
                RegisterErrorMessage = result.Message;
                return;
            }

            _emailService.InvalidateCode(RegisterEmail);

            string registeredUsername = result.Data.Tendangnhap ?? string.Empty;

            RegisterHoTen = string.Empty;
            RegisterSDT = string.Empty;
            RegisterEmail = string.Empty;
            RegisterDiaChi = string.Empty;
            RegisterUsername = string.Empty;
            RegisterPassword = string.Empty;
            RegisterConfirmPassword = string.Empty;
            RegisterVerificationCode = string.Empty;
            VerificationInfoMessage = string.Empty;
            ClearRegisterValidationErrors();
            IsLoginMode = false;

            LoginUsername = registeredUsername;
            LoginPassword = string.Empty;
            LoginErrorMessage = string.Empty;
            LoginSuccessMessage = result.Message;
            IsLoginMode = true;

            _dialogService.ShowInformation("Đăng ký thành công", result.Message);
        }

        partial void OnRegisterEmailChanged(string value)
        {
            RegisterVerificationCode = string.Empty;
            VerificationInfoMessage = string.Empty;
        }

        partial void OnRegisterVerificationCodeChanged(string value)
        {
            if (HasVerificationCodeError)
            {
                VerificationCodeError = string.Empty;
            }
        }

        partial void OnRegisterPasswordChanged(string value)
        {
            if (HasPasswordError)
            {
                PasswordError = string.Empty;
            }
        }

        partial void OnRegisterConfirmPasswordChanged(string value)
        {
            if (HasConfirmPasswordError)
            {
                ConfirmPasswordError = string.Empty;
            }
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

            if (string.IsNullOrWhiteSpace(RegisterEmail))
            {
                EmailError = "Vui lòng nhập email.";
                valid = false;
            }
            else if (!RegisterEmail.EndsWith("@gmail.com") || RegisterEmail.Length < 10)
            {
                EmailError = "Vui lòng nhập đúng định dạng @gmail.com và độ dài lớn hơn hoặc bằng 10";
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(RegisterDiaChi))
            {
                DiaChiError = "Vui lòng nhập địa chỉ nhận sách.";
                valid = false;
            }

            string trimmedUsername = RegisterUsername?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmedUsername) || trimmedUsername.Length < 3)
            {
                UsernameError = "Tên đăng nhập phải có ít nhất 3 ký tự (tối đa 10 ký tự).";
                valid = false;
            }
            else if (trimmedUsername.Length > 10)
            {
                UsernameError = UsernameTooLongInlineError;
                RegisterErrorMessage = UsernameTooLongBannerError;
                valid = false;
            }
            else if (trimmedUsername.Contains(' '))
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
            EmailError = string.Empty;
            VerificationCodeError = string.Empty;
            DiaChiError = string.Empty;
            UsernameError = string.Empty;
            PasswordError = string.Empty;
            ConfirmPasswordError = string.Empty;
            RegisterErrorMessage = string.Empty;
        }
    }
}
