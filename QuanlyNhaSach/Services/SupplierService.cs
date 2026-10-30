using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using QuanlyNhaSach.Models;

namespace QuanlyNhaSach.Services
{
    public class SupplierService
    {
        private static readonly Regex PhoneRegex = new(@"^\d{9,11}$", RegexOptions.Compiled);
        private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        private readonly MockDataStore _dataStore;

        public event EventHandler? SuppliersChanged;

        public SupplierService(MockDataStore dataStore)
        {
            _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
        }

        public IReadOnlyList<Supplier> GetAllSuppliers(bool includeInactive = true)
        {
            var query = includeInactive
                ? _dataStore.Suppliers.AsEnumerable()
                : _dataStore.Suppliers.Where(s => s.IsActive);

            return query.OrderBy(s => s.MaNCC).ToList();
        }

        public IReadOnlyList<Supplier> GetActiveSuppliers()
        {
            return GetAllSuppliers(includeInactive: false);
        }

        public Supplier? GetSupplierById(int maNCC)
        {
            return _dataStore.GetSupplierById(maNCC);
        }

        public IReadOnlyList<Supplier> SearchSuppliers(string? keyword, bool includeInactive = true)
        {
            var all = GetAllSuppliers(includeInactive);
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return all;
            }

            string kw = keyword.Trim();
            string cleanId = kw.TrimStart('#');
            if (cleanId.StartsWith("NCC", StringComparison.OrdinalIgnoreCase) && cleanId.Length > 3)
            {
                cleanId = cleanId.Substring(3).TrimStart('0');
                if (string.IsNullOrEmpty(cleanId))
                {
                    cleanId = "0";
                }
            }

            return all.Where(s =>
                s.TenNCC.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                s.SDT.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                s.Email.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                s.DiaChi.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                s.SupplierCode.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                s.MaNCC.ToString().Equals(cleanId, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public OperationResult<Supplier> AddSupplier(
            string? tenNCC,
            string? sdt,
            string? email,
            string? diaChi)
        {
            var validation = ValidateSupplierInput(tenNCC, sdt, email, diaChi);
            if (!validation.IsSuccess)
            {
                return OperationResult<Supplier>.Failure(validation.Message);
            }

            string normalizedName = tenNCC!.Trim();
            string normalizedPhone = sdt!.Trim();
            string normalizedEmail = email!.Trim();

            bool duplicateExists = _dataStore.Suppliers.Any(s =>
                s.TenNCC.Equals(normalizedName, StringComparison.OrdinalIgnoreCase) ||
                s.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase));

            if (duplicateExists)
            {
                return OperationResult<Supplier>.Failure(
                    "Nhà cung cấp có cùng tên hoặc địa chỉ Email này đã tồn tại trong hệ thống.");
            }

            var newSupplier = new Supplier
            {
                MaNCC = _dataStore.GenerateNextSupplierId(),
                TenNCC = normalizedName,
                SDT = normalizedPhone,
                Email = normalizedEmail,
                DiaChi = diaChi!.Trim(),
                IsActive = true
            };

            _dataStore.Suppliers.Add(newSupplier);
            NotifyChanged();

            return OperationResult<Supplier>.Success(
                newSupplier,
                $"Đã thêm nhà cung cấp \"{newSupplier.TenNCC}\" ({newSupplier.SupplierCode}) thành công.");
        }

        public OperationResult<Supplier> UpdateSupplier(
            int maNCC,
            string? tenNCC,
            string? sdt,
            string? email,
            string? diaChi,
            bool isActive = true)
        {
            var supplier = _dataStore.GetSupplierById(maNCC);
            if (supplier == null)
            {
                return OperationResult<Supplier>.Failure($"Không tìm thấy nhà cung cấp #{maNCC}.");
            }

            var validation = ValidateSupplierInput(tenNCC, sdt, email, diaChi);
            if (!validation.IsSuccess)
            {
                return OperationResult<Supplier>.Failure(validation.Message);
            }

            string normalizedName = tenNCC!.Trim();
            string normalizedEmail = email!.Trim();

            bool duplicateExists = _dataStore.Suppliers.Any(s =>
                s.MaNCC != maNCC &&
                (s.TenNCC.Equals(normalizedName, StringComparison.OrdinalIgnoreCase) ||
                 s.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase)));

            if (duplicateExists)
            {
                return OperationResult<Supplier>.Failure(
                    "Tên nhà cung cấp hoặc Email đã được sử dụng bởi một nhà cung cấp khác.");
            }

            // Update supplier in-place; historical ImportReceipt records retain their original TenNCC snapshot
            supplier.TenNCC = normalizedName;
            supplier.SDT = sdt!.Trim();
            supplier.Email = normalizedEmail;
            supplier.DiaChi = diaChi!.Trim();
            supplier.IsActive = isActive;

            NotifyChanged();

            return OperationResult<Supplier>.Success(
                supplier,
                $"Đã cập nhật thông tin nhà cung cấp \"{supplier.TenNCC}\" ({supplier.SupplierCode}).");
        }

        public OperationResult ToggleOrDeactivateSupplier(int maNCC)
        {
            var supplier = _dataStore.GetSupplierById(maNCC);
            if (supplier == null)
            {
                return OperationResult.Failure($"Không tìm thấy nhà cung cấp #{maNCC}.");
            }

            bool referencedByReceipts = _dataStore.ImportReceipts.Any(r => r.MaNCC == maNCC);

            if (!supplier.IsActive)
            {
                supplier.IsActive = true;
                NotifyChanged();
                return OperationResult.Success(
                    $"Đã kích hoạt lại trạng thái hợp tác với nhà cung cấp \"{supplier.TenNCC}\" ({supplier.SupplierCode}).");
            }

            if (referencedByReceipts)
            {
                supplier.IsActive = false;
                NotifyChanged();
                return OperationResult.Success(
                    $"Nhà cung cấp \"{supplier.TenNCC}\" ({supplier.SupplierCode}) đang được tham chiếu trong lịch sử phiếu nhập hàng.\n\n" +
                    $"Để bảo vệ toàn vẹn dữ liệu lịch sử, hệ thống đã chuyển nhà cung cấp sang trạng thái \"Ngừng hợp tác\" thay vì xóa vật lý.");
            }

            supplier.IsActive = false;
            NotifyChanged();
            return OperationResult.Success(
                $"Đã chuyển trạng thái nhà cung cấp \"{supplier.TenNCC}\" ({supplier.SupplierCode}) sang \"Ngừng hợp tác\".");
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

            if (string.IsNullOrWhiteSpace(sdt) || !PhoneRegex.IsMatch(sdt.Trim()))
            {
                return OperationResult.Failure("Số điện thoại nhà cung cấp không hợp lệ (vui lòng nhập từ 9 đến 11 chữ số).");
            }

            if (string.IsNullOrWhiteSpace(email) || !EmailRegex.IsMatch(email.Trim()))
            {
                return OperationResult.Failure("Địa chỉ Email nhà cung cấp không đúng định dạng (ví dụ: contact@nhaxuatban.vn).");
            }

            if (string.IsNullOrWhiteSpace(diaChi))
            {
                return OperationResult.Failure("Vui lòng nhập địa chỉ trụ sở nhà cung cấp.");
            }

            return OperationResult.Success();
        }

        private void NotifyChanged()
        {
            SuppliersChanged?.Invoke(this, EventArgs.Empty);
            _dataStore.NotifyDataChanged();
        }
    }
}
