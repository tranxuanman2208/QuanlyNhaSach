using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using QuanlyNhaSach.Models;

namespace QuanlyNhaSach.Services
{
    public class SupplierService
    {
        // SQL Server column NHACUNGCAP.SDT is varchar(10)
        private static readonly Regex PhoneRegex = new(@"^\d{9,10}$", RegexOptions.Compiled);
        private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        // Session-level soft deactivation tracker for suppliers referenced by historical records
        private static readonly HashSet<string> DeactivatedSupplierIds = new(StringComparer.OrdinalIgnoreCase);

        public event EventHandler? SuppliersChanged;

        public SupplierService()
        {
        }

        public IReadOnlyList<NHACUNGCAP> GetAllSuppliers(bool includeInactive = true)
        {
            try
            {
                using var db = new QuanlyNhaSachContext();
                var list = db.NHACUNGCAPs
                    .AsNoTracking()
                    .OrderBy(s => s.MaNCC)
                    .ToList();

                foreach (var supplier in list)
                {
                    supplier.MaNCC = supplier.MaNCC?.Trim() ?? string.Empty;
                    supplier.TenNCC = supplier.TenNCC?.Trim() ?? string.Empty;
                    supplier.SDT = supplier.SDT?.Trim() ?? string.Empty;
                    supplier.Email = supplier.Email?.Trim() ?? string.Empty;
                    supplier.Diachi = supplier.Diachi?.Trim() ?? string.Empty;
                    supplier.IsActive = !DeactivatedSupplierIds.Contains(supplier.MaNCC);
                }

                if (!includeInactive)
                {
                    list = list.Where(s => s.IsActive).ToList();
                }

                return list;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi tải danh sách Nhà cung cấp: {ex.Message}");
                return Array.Empty<NHACUNGCAP>();
            }
        }

        public IReadOnlyList<NHACUNGCAP> GetActiveSuppliers()
        {
            return GetAllSuppliers(includeInactive: false);
        }

        public NHACUNGCAP? GetSupplierById(string? maNCC)
        {
            if (string.IsNullOrWhiteSpace(maNCC))
            {
                return null;
            }

            try
            {
                string key = maNCC.Trim();
                using var db = new QuanlyNhaSachContext();
                var supplier = db.NHACUNGCAPs
                    .AsNoTracking()
                    .FirstOrDefault(s => s.MaNCC == key);

                if (supplier != null)
                {
                    supplier.MaNCC = supplier.MaNCC.Trim();
                    supplier.TenNCC = supplier.TenNCC?.Trim() ?? string.Empty;
                    supplier.SDT = supplier.SDT?.Trim() ?? string.Empty;
                    supplier.Email = supplier.Email?.Trim() ?? string.Empty;
                    supplier.Diachi = supplier.Diachi?.Trim() ?? string.Empty;
                    supplier.IsActive = !DeactivatedSupplierIds.Contains(supplier.MaNCC);
                }

                return supplier;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi tìm Nhà cung cấp {maNCC}: {ex.Message}");
                return null;
            }
        }

        public IReadOnlyList<NHACUNGCAP> SearchSuppliers(string? keyword, bool includeInactive = true)
        {
            var all = GetAllSuppliers(includeInactive);
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return all;
            }

            string kw = keyword.Trim();
            string cleanId = kw.TrimStart('#');

            return all.Where(s =>
                (s.TenNCC?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (s.SDT?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (s.Email?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (s.DiaChi?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false) ||
                s.SupplierCode.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                s.MaNCC.Contains(cleanId, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public OperationResult<NHACUNGCAP> AddSupplier(
            string? tenNCC,
            string? sdt,
            string? email,
            string? diaChi)
        {
            var validation = ValidateSupplierInput(tenNCC, sdt, email, diaChi);
            if (!validation.IsSuccess)
            {
                return OperationResult<NHACUNGCAP>.Failure(validation.Message);
            }

            string normalizedName = tenNCC!.Trim();
            string normalizedPhone = sdt!.Trim();
            string normalizedEmail = email!.Trim();
            string normalizedAddress = diaChi!.Trim();

            try
            {
                using var db = new QuanlyNhaSachContext();

                bool duplicateExists = db.NHACUNGCAPs
                    .AsNoTracking()
                    .Any(s =>
                        (s.TenNCC != null && s.TenNCC.ToLower() == normalizedName.ToLower()) ||
                        (s.Email != null && s.Email.ToLower() == normalizedEmail.ToLower()));

                if (duplicateExists)
                {
                    return OperationResult<NHACUNGCAP>.Failure(
                        "Nhà cung cấp có cùng tên hoặc địa chỉ Email này đã tồn tại trong hệ thống.");
                }

                string newMaNCC = GenerateNextSupplierId(db);

                var newSupplier = new NHACUNGCAP
                {
                    MaNCC = newMaNCC,
                    TenNCC = normalizedName,
                    SDT = normalizedPhone,
                    Email = normalizedEmail,
                    Diachi = normalizedAddress,
                    IsActive = true
                };

                db.NHACUNGCAPs.Add(newSupplier);
                db.SaveChanges();

                NotifyChanged();
                return OperationResult<NHACUNGCAP>.Success(
                    newSupplier,
                    $"Đã thêm nhà cung cấp \"{newSupplier.TenNCC}\" ({newSupplier.SupplierCode}) thành công.");
            }
            catch (Exception ex)
            {
                return OperationResult<NHACUNGCAP>.Failure(
                    $"Không thể thêm nhà cung cấp vào cơ sở dữ liệu: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        public OperationResult<NHACUNGCAP> UpdateSupplier(
            string? maNCC,
            string? tenNCC,
            string? sdt,
            string? email,
            string? diaChi,
            bool isActive = true)
        {
            if (string.IsNullOrWhiteSpace(maNCC))
            {
                return OperationResult<NHACUNGCAP>.Failure("Mã nhà cung cấp không hợp lệ.");
            }

            var validation = ValidateSupplierInput(tenNCC, sdt, email, diaChi);
            if (!validation.IsSuccess)
            {
                return OperationResult<NHACUNGCAP>.Failure(validation.Message);
            }

            string key = maNCC.Trim();
            string normalizedName = tenNCC!.Trim();
            string normalizedPhone = sdt!.Trim();
            string normalizedEmail = email!.Trim();
            string normalizedAddress = diaChi!.Trim();

            try
            {
                using var db = new QuanlyNhaSachContext();
                var supplier = db.NHACUNGCAPs.FirstOrDefault(s => s.MaNCC == key);
                if (supplier == null)
                {
                    return OperationResult<NHACUNGCAP>.Failure($"Không tìm thấy nhà cung cấp #{key}.");
                }

                bool duplicateExists = db.NHACUNGCAPs
                    .AsNoTracking()
                    .Any(s =>
                        s.MaNCC != key &&
                        ((s.TenNCC != null && s.TenNCC.ToLower() == normalizedName.ToLower()) ||
                         (s.Email != null && s.Email.ToLower() == normalizedEmail.ToLower())));

                if (duplicateExists)
                {
                    return OperationResult<NHACUNGCAP>.Failure(
                        "Tên nhà cung cấp hoặc Email đã được sử dụng bởi một nhà cung cấp khác.");
                }

                supplier.TenNCC = normalizedName;
                supplier.SDT = normalizedPhone;
                supplier.Email = normalizedEmail;
                supplier.Diachi = normalizedAddress;
                supplier.IsActive = isActive;

                if (isActive)
                {
                    DeactivatedSupplierIds.Remove(key);
                }
                else
                {
                    DeactivatedSupplierIds.Add(key);
                }

                db.SaveChanges();
                NotifyChanged();

                return OperationResult<NHACUNGCAP>.Success(
                    supplier,
                    $"Đã cập nhật thông tin nhà cung cấp \"{supplier.TenNCC}\" ({supplier.SupplierCode}) thành công.");
            }
            catch (Exception ex)
            {
                return OperationResult<NHACUNGCAP>.Failure(
                    $"Không thể cập nhật nhà cung cấp: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        public OperationResult ToggleOrDeactivateSupplier(string? maNCC)
        {
            if (string.IsNullOrWhiteSpace(maNCC))
            {
                return OperationResult.Failure("Mã nhà cung cấp không hợp lệ.");
            }

            string key = maNCC.Trim();

            try
            {
                using var db = new QuanlyNhaSachContext();
                var supplier = db.NHACUNGCAPs
                    .AsNoTracking()
                    .FirstOrDefault(s => s.MaNCC == key);

                if (supplier == null)
                {
                    return OperationResult.Failure($"Không tìm thấy nhà cung cấp #{key}.");
                }

                bool currentlyActive = !DeactivatedSupplierIds.Contains(key);
                if (currentlyActive)
                {
                    DeactivatedSupplierIds.Add(key);
                    NotifyChanged();
                    return OperationResult.Success(
                        $"Đã chuyển nhà cung cấp \"{supplier.TenNCC}\" ({supplier.SupplierCode}) sang trạng thái Ngừng hợp tác. Các sách và phiếu nhập lịch sử vẫn được bảo toàn.");
                }
                else
                {
                    DeactivatedSupplierIds.Remove(key);
                    NotifyChanged();
                    return OperationResult.Success(
                        $"Đã kích hoạt lại nhà cung cấp \"{supplier.TenNCC}\" ({supplier.SupplierCode}).");
                }
            }
            catch (Exception ex)
            {
                return OperationResult.Failure(
                    $"Không thể thay đổi trạng thái nhà cung cấp: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        public OperationResult DeleteSupplier(string? maNCC)
        {
            if (string.IsNullOrWhiteSpace(maNCC))
            {
                return OperationResult.Failure("Mã nhà cung cấp không hợp lệ.");
            }

            string key = maNCC.Trim();

            try
            {
                using var db = new QuanlyNhaSachContext();
                var supplier = db.NHACUNGCAPs.FirstOrDefault(s => s.MaNCC == key);
                if (supplier == null)
                {
                    return OperationResult.Failure($"Không tìm thấy nhà cung cấp #{key}.");
                }

                bool hasBooks = db.SACHs.AsNoTracking().Any(b => b.MaNCC == key);
                bool hasReceipts = db.PHIEUNHAPs.AsNoTracking().Any(p => p.MaNCC == key);

                if (hasBooks || hasReceipts)
                {
                    return OperationResult.Failure(
                        $"Không thể xóa nhà cung cấp \"{supplier.TenNCC}\" vì đang có sách hoặc phiếu nhập tham chiếu đến mã {key}.");
                }

                db.NHACUNGCAPs.Remove(supplier);
                db.SaveChanges();
                DeactivatedSupplierIds.Remove(key);

                NotifyChanged();
                return OperationResult.Success($"Đã xóa nhà cung cấp \"{supplier.TenNCC}\" khỏi cơ sở dữ liệu.");
            }
            catch (Exception ex)
            {
                return OperationResult.Failure(
                    $"Không thể xóa nhà cung cấp: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private static string GenerateNextSupplierId(QuanlyNhaSachContext db)
        {
            var existingIds = db.NHACUNGCAPs
                .AsNoTracking()
                .Select(s => s.MaNCC)
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
            string candidate = $"NCC{next:D4}";
            while (existingIds.Any(x => string.Equals(x?.Trim(), candidate, StringComparison.OrdinalIgnoreCase)))
            {
                next++;
                candidate = $"NCC{next:D4}";
            }

            return candidate;
        }

        private static OperationResult ValidateSupplierInput(
            string? tenNCC,
            string? sdt,
            string? email,
            string? diaChi)
        {
            if (string.IsNullOrWhiteSpace(tenNCC))
            {
                return OperationResult.Failure("Vui lòng nhập tên nhà cung cấp.");
            }

            if (tenNCC.Trim().Length > 100)
            {
                return OperationResult.Failure("Tên nhà cung cấp không được vượt quá 100 ký tự.");
            }

            if (string.IsNullOrWhiteSpace(sdt) || !PhoneRegex.IsMatch(sdt.Trim()))
            {
                return OperationResult.Failure("Số điện thoại nhà cung cấp không hợp lệ (vui lòng nhập 9 hoặc 10 chữ số).");
            }

            if (string.IsNullOrWhiteSpace(email) || !EmailRegex.IsMatch(email.Trim()) || email.Trim().Length > 100)
            {
                return OperationResult.Failure("Địa chỉ Email nhà cung cấp không đúng định dạng (ví dụ: contact@nhaxuatban.vn).");
            }

            if (string.IsNullOrWhiteSpace(diaChi))
            {
                return OperationResult.Failure("Vui lòng nhập địa chỉ trụ sở nhà cung cấp.");
            }

            if (diaChi.Trim().Length > 100)
            {
                return OperationResult.Failure("Địa chỉ nhà cung cấp không được vượt quá 100 ký tự.");
            }

            return OperationResult.Success();
        }

        private void NotifyChanged()
        {
            SuppliersChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
