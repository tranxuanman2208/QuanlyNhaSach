using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using QuanlyNhaSach.Models;
using QuanlyNhaSach.Models.Enums;

namespace QuanlyNhaSach.Services
{
    /// <summary>
    /// Cấu hình SMTP dùng để gửi email. Được đọc từ file "emailsettings.json"
    /// nằm cạnh file thực thi (.exe).
    /// </summary>
    public class EmailSettings
    {
        public string SmtpHost { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderName { get; set; } = "Nhà sách";
        /// <summary>Mật khẩu ứng dụng (App Password) của Gmail, KHÔNG phải mật khẩu đăng nhập.</summary>
        public string AppPassword { get; set; } = string.Empty;
        public int CodeLength { get; set; } = 6;
        public int CodeExpiryMinutes { get; set; } = 10;
        public int MaxVerifyAttempts { get; set; } = 5;
        public int ResendCooldownSeconds { get; set; } = 30;

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(SmtpHost) &&
            !string.IsNullOrWhiteSpace(SenderEmail) &&
            !string.IsNullOrWhiteSpace(AppPassword);
    }

    /// <summary>
    /// Dịch vụ gửi email: sinh và gửi mã xác thực khi tạo tài khoản Khách hàng / Nhân viên,
    /// đồng thời lưu mã (trong bộ nhớ) để kiểm tra lại sau này.
    /// </summary>
    public class EmailService
    {
        public const string SettingsFileName = "emailsettings.json";

        private sealed class PendingCode
        {
            public required string CodeHash { get; init; }
            public required DateTime ExpiresAtUtc { get; init; }
            public required DateTime SentAtUtc { get; init; }
            public int FailedAttempts { get; set; }
        }

        private readonly EmailSettings _settings;
        private readonly ConcurrentDictionary<string, PendingCode> _pendingCodes =
            new(StringComparer.OrdinalIgnoreCase);

        public EmailService() : this(LoadSettings())
        {
        }

        public EmailService(EmailSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public bool IsConfigured => _settings.IsConfigured;

        public static EmailSettings LoadSettings()
        {
            try
            {
                string path = Path.Combine(AppContext.BaseDirectory, SettingsFileName);
                if (!File.Exists(path))
                {
                    System.Diagnostics.Debug.WriteLine($"Không tìm thấy file cấu hình email: {path}");
                    return new EmailSettings();
                }

                string json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<EmailSettings>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                }) ?? new EmailSettings();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi đọc cấu hình email: {ex.Message}");
                return new EmailSettings();
            }
        }

        /// <summary>
        /// Sinh mã xác thực mới cho <paramref name="toEmail"/> và gửi đến hộp thư đó.
        /// Data trả về là mã đã gửi (chỉ dùng cho mục đích ghi log / kiểm thử).
        /// </summary>
        public async Task<OperationResult<string>> SendVerificationCodeAsync(
            string? toEmail,
            string? fullName,
            UserRole role,
            string? username = null)
        {
            if (string.IsNullOrWhiteSpace(toEmail) || !MailAddress.TryCreate(toEmail.Trim(), out var recipient))
            {
                return OperationResult<string>.Failure("Địa chỉ email nhận mã xác thực không hợp lệ.");
            }

            if (!_settings.IsConfigured)
            {
                return OperationResult<string>.Failure(
                    $"Chưa cấu hình tài khoản gửi email (SenderEmail / AppPassword) trong file \"{SettingsFileName}\".");
            }

            string key = NormalizeEmail(recipient.Address);

            if (_pendingCodes.TryGetValue(key, out var previous))
            {
                int waitSeconds = (int)Math.Ceiling(
                    (previous.SentAtUtc.AddSeconds(_settings.ResendCooldownSeconds) - DateTime.UtcNow).TotalSeconds);
                if (waitSeconds > 0)
                {
                    return OperationResult<string>.Failure(
                        $"Mã xác thực vừa được gửi. Vui lòng đợi {waitSeconds} giây trước khi gửi lại.");
                }
            }

            string code = GenerateCode(_settings.CodeLength);
            _pendingCodes[key] = new PendingCode
            {
                CodeHash = HashCode(code),
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(_settings.CodeExpiryMinutes),
                SentAtUtc = DateTime.UtcNow
            };

            string roleLabel = role switch
            {
                UserRole.Staff => "Nhân viên",
                UserRole.Manager => "Quản lý",
                _ => "Khách hàng"
            };

            string displayName = string.IsNullOrWhiteSpace(fullName) ? recipient.Address : fullName.Trim();
            string subject = $"[{_settings.SenderName}] Mã xác thực tài khoản {roleLabel}";
            string body = BuildVerificationBody(displayName, roleLabel, username, code, _settings.CodeExpiryMinutes);

            try
            {
                using var message = new MailMessage
                {
                    From = new MailAddress(_settings.SenderEmail, _settings.SenderName, Encoding.UTF8),
                    Subject = subject,
                    SubjectEncoding = Encoding.UTF8,
                    Body = body,
                    BodyEncoding = Encoding.UTF8,
                    IsBodyHtml = true
                };
                message.To.Add(recipient);

                string cleanAppPass = (_settings.AppPassword ?? string.Empty).Replace(" ", "").Trim();
                using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
                {
                    EnableSsl = _settings.EnableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_settings.SenderEmail, cleanAppPass),
                    Timeout = 20000
                };

                await client.SendMailAsync(message);

                return OperationResult<string>.Success(
                    code,
                    $"Mã xác thực đã được gửi đến email {recipient.Address} (hiệu lực {_settings.CodeExpiryMinutes} phút).");
            }
            catch (Exception ex)
            {
                _pendingCodes.TryRemove(key, out _);
                return OperationResult<string>.Failure(
                    $"Không thể gửi mã xác thực đến {recipient.Address}: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        /// <summary>
        /// Kiểm tra mã xác thực người dùng nhập. Mã chỉ dùng được một lần:
        /// mặc định bị hủy ngay khi khớp; truyền <paramref name="consumeOnSuccess"/> = false
        /// để giữ lại mã (chờ lưu tài khoản thành công rồi mới gọi <see cref="InvalidateCode"/>).
        /// </summary>
        public OperationResult VerifyCode(string? email, string? code, bool consumeOnSuccess = true)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
            {
                return OperationResult.Failure("Vui lòng nhập email và mã xác thực.");
            }

            string key = NormalizeEmail(email);
            if (!_pendingCodes.TryGetValue(key, out var pending))
            {
                return OperationResult.Failure("Không có mã xác thực nào đang chờ cho email này. Vui lòng yêu cầu gửi lại mã.");
            }

            if (DateTime.UtcNow > pending.ExpiresAtUtc)
            {
                _pendingCodes.TryRemove(key, out _);
                return OperationResult.Failure("Mã xác thực đã hết hạn. Vui lòng yêu cầu gửi lại mã.");
            }

            bool match = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(pending.CodeHash),
                Encoding.UTF8.GetBytes(HashCode(code.Trim())));

            if (!match)
            {
                pending.FailedAttempts++;
                if (pending.FailedAttempts >= _settings.MaxVerifyAttempts)
                {
                    _pendingCodes.TryRemove(key, out _);
                    return OperationResult.Failure("Bạn đã nhập sai quá số lần cho phép. Vui lòng yêu cầu gửi lại mã.");
                }

                int remaining = _settings.MaxVerifyAttempts - pending.FailedAttempts;
                return OperationResult.Failure($"Mã xác thực không đúng. Bạn còn {remaining} lần thử.");
            }

            if (consumeOnSuccess)
            {
                _pendingCodes.TryRemove(key, out _);
            }
            return OperationResult.Success("Xác thực email thành công.");
        }

        /// <summary>Hủy mã xác thực đang chờ của email (sau khi đã dùng xong).</summary>
        public void InvalidateCode(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return;
            _pendingCodes.TryRemove(NormalizeEmail(email), out _);
        }

        public bool HasPendingCode(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return _pendingCodes.TryGetValue(NormalizeEmail(email), out var p) && DateTime.UtcNow <= p.ExpiresAtUtc;
        }

        private static string GenerateCode(int length)
        {
            if (length < 4) length = 4;
            if (length > 10) length = 10;

            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                sb.Append(RandomNumberGenerator.GetInt32(0, 10));
            }
            return sb.ToString();
        }

        private static string HashCode(string code)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
        }

        private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

        private static string BuildVerificationBody(
            string displayName,
            string roleLabel,
            string? username,
            string code,
            int expiryMinutes)
        {
            string safeName = WebUtility.HtmlEncode(displayName);
            string usernameRow = string.IsNullOrWhiteSpace(username)
                ? string.Empty
                : $"<p style=\"margin:0 0 8px\">Tên đăng nhập: <b>{WebUtility.HtmlEncode(username)}</b></p>";

            return $@"
<div style=""font-family:Segoe UI,Arial,sans-serif;max-width:520px;margin:auto;padding:24px;border:1px solid #e5e7eb;border-radius:12px"">
  <h2 style=""margin:0 0 16px;color:#1f2937"">Xác thực tài khoản {roleLabel}</h2>
  <p style=""margin:0 0 8px"">Xin chào <b>{safeName}</b>,</p>
  <p style=""margin:0 0 8px"">Tài khoản {roleLabel.ToLowerInvariant()} của bạn tại hệ thống Quản lý Nhà sách vừa được tạo.</p>
  {usernameRow}
  <p style=""margin:16px 0 8px"">Mã xác thực của bạn là:</p>
  <div style=""font-size:32px;font-weight:700;letter-spacing:8px;text-align:center;padding:16px;background:#f3f4f6;border-radius:8px;color:#111827"">{code}</div>
  <p style=""margin:16px 0 8px;color:#6b7280"">Mã có hiệu lực trong <b>{expiryMinutes} phút</b>. Vui lòng không chia sẻ mã này cho bất kỳ ai.</p>
  <p style=""margin:0;color:#9ca3af;font-size:12px"">Nếu bạn không thực hiện yêu cầu này, hãy bỏ qua email.</p>
</div>";
        }
    }
}
