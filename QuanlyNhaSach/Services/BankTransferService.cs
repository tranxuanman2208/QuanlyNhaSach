using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QuanlyNhaSach.Services
{
    public class BankSettings
    {
        public string BankId { get; set; } = "MB";
        public string BankName { get; set; } = "Ngân hàng TMCP Quân Đội (MB Bank)";
        public string AccountNo { get; set; } = "0386543210";
        public string AccountName { get; set; } = "TRAN XUAN MAN";
        public string Template { get; set; } = "compact2";
        public string CustomQrImagePath { get; set; } = string.Empty;
    }

    public class BankTransferService
    {
        public const string SettingsFileName = "banksettings.json";
        private static readonly HttpClient SharedHttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        private BankSettings _settings;

        public BankTransferService()
        {
            _settings = LoadSettings();
        }

        public BankSettings Settings => _settings;

        public void ReloadSettings()
        {
            _settings = LoadSettings();
        }

        public static BankSettings LoadSettings()
        {
            try
            {
                string[] candidatePaths =
                {
                    Path.Combine(AppContext.BaseDirectory, SettingsFileName),
                    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..", SettingsFileName)),
                    Path.Combine(Directory.GetCurrentDirectory(), SettingsFileName)
                };

                foreach (string path in candidatePaths)
                {
                    if (File.Exists(path))
                    {
                        string json = File.ReadAllText(path);
                        var parsed = JsonSerializer.Deserialize<BankSettings>(json, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            ReadCommentHandling = JsonCommentHandling.Skip,
                            AllowTrailingCommas = true
                        });

                        if (parsed != null)
                        {
                            return parsed;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi đọc cấu hình ngân hàng: {ex.Message}");
            }

            return new BankSettings();
        }

        public string BuildTransferContent(int? maDH, string? customerPhone)
        {
            string codePart = maDH.HasValue && maDH.Value > 0
                ? $"DH{maDH.Value:D4}"
                : $"DH{DateTime.Now:HHmmss}";

            string cleanPhone = string.IsNullOrWhiteSpace(customerPhone)
                ? string.Empty
                : Regex.Replace(customerPhone.Trim(), @"\D", "");

            return string.IsNullOrWhiteSpace(cleanPhone)
                ? $"TT {codePart}"
                : $"TT {codePart} {cleanPhone}";
        }

        public string BuildVietQrUrl(decimal amount, string transferContent)
        {
            ReloadSettings();

            string bankId = string.IsNullOrWhiteSpace(_settings.BankId) ? "MB" : _settings.BankId.Trim();
            string accountNo = string.IsNullOrWhiteSpace(_settings.AccountNo) ? "0386543210" : _settings.AccountNo.Trim();
            string template = string.IsNullOrWhiteSpace(_settings.Template) ? "compact2" : _settings.Template.Trim();
            string accountName = string.IsNullOrWhiteSpace(_settings.AccountName) ? "CHU CUA HANG" : _settings.AccountName.Trim();

            long roundedAmount = (long)Math.Max(0m, Math.Round(amount, 0));
            string encodedInfo = Uri.EscapeDataString(transferContent ?? string.Empty);
            string encodedName = Uri.EscapeDataString(accountName);

            return $"https://img.vietqr.io/image/{bankId}-{accountNo}-{template}.png?amount={roundedAmount}&addInfo={encodedInfo}&accountName={encodedName}";
        }

        public string? ResolveLocalCustomQrPath()
        {
            ReloadSettings();

            if (!string.IsNullOrWhiteSpace(_settings.CustomQrImagePath))
            {
                string custom = _settings.CustomQrImagePath.Trim();
                string[] customCandidates =
                {
                    custom,
                    Path.Combine(AppContext.BaseDirectory, custom),
                    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..", custom))
                };

                foreach (string candidate in customCandidates)
                {
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
            }

            string[] autoFileNames = { "qr_ca_nhan.png", "qr_ca_nhan.jpg", "qr_ca_nhan.jpeg", "qr.png", "qr.jpg" };
            string[] imgDirs =
            {
                Path.Combine(AppContext.BaseDirectory, "Img"),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\Img"))
            };

            foreach (string dir in imgDirs)
            {
                if (!Directory.Exists(dir)) continue;
                foreach (string fileName in autoFileNames)
                {
                    string fullPath = Path.Combine(dir, fileName);
                    if (File.Exists(fullPath))
                    {
                        return fullPath;
                    }
                }
            }

            return null;
        }

        public async Task<ImageSource?> LoadQrImageAsync(decimal amount, string transferContent, CancellationToken cancellationToken = default)
        {
            string? localPath = ResolveLocalCustomQrPath();
            if (!string.IsNullOrWhiteSpace(localPath) && File.Exists(localPath))
            {
                try
                {
                    byte[] localBytes = await File.ReadAllBytesAsync(localPath, cancellationToken);
                    return CreateFrozenBitmap(localBytes);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Lỗi đọc ảnh QR cá nhân nội bộ: {ex.Message}");
                }
            }

            string vietQrUrl = BuildVietQrUrl(amount, transferContent);
            try
            {
                byte[] bytes = await SharedHttpClient.GetByteArrayAsync(vietQrUrl, cancellationToken);
                if (bytes.Length > 0)
                {
                    return CreateFrozenBitmap(bytes);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Không thể tải ảnh VietQR trực tuyến: {ex.Message}");
            }

            return null;
        }

        private static BitmapImage CreateFrozenBitmap(byte[] imageBytes)
        {
            using var ms = new MemoryStream(imageBytes);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = ms;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
    }
}
