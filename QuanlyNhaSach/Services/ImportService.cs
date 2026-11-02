using System;
using System.Collections.Generic;
using System.Linq;
using QuanlyNhaSach.Models;
using QuanlyNhaSach.Models.Enums;

namespace QuanlyNhaSach.Services
{
    public class ImportService
    {
        private readonly MockDataStore _dataStore;

        public event EventHandler<ImportReceipt>? ImportReceiptConfirmed;

        public ImportService(MockDataStore dataStore)
        {
            _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
        }

        public int PeekNextReceiptId() => _dataStore.PeekNextImportReceiptId();

        public IReadOnlyList<ImportReceipt> GetConfirmedReceipts()
        {
            return _dataStore.ImportReceipts
                .Where(r => r.TrangThai == ImportReceiptStatus.Confirmed)
                .OrderByDescending(r => r.NgayLap)
                .ThenByDescending(r => r.MaPN)
                .ToList();
        }

        public IReadOnlyList<ImportReceipt> SearchConfirmedReceipts(
            string? keyword,
            int? supplierId = null)
        {
            IEnumerable<ImportReceipt> query = GetConfirmedReceipts();

            if (supplierId.HasValue && supplierId.Value > 0)
            {
                query = query.Where(r => r.MaNCC == supplierId.Value);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string kw = keyword.Trim();
                string cleanId = kw.TrimStart('#');
                if (cleanId.StartsWith("PN", StringComparison.OrdinalIgnoreCase) && cleanId.Length > 2)
                {
                    cleanId = cleanId.Substring(2).TrimStart('0');
                    if (string.IsNullOrEmpty(cleanId))
                    {
                        cleanId = "0";
                    }
                }

                query = query.Where(r =>
                    r.ReceiptCode.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    r.MaPN.ToString().Equals(cleanId, StringComparison.OrdinalIgnoreCase) ||
                    r.TenNCC.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    r.NguoiLapHoTen.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    (r.Details != null && r.Details.Any(d =>
                        d.TenSach.Contains(kw, StringComparison.OrdinalIgnoreCase))));
            }

            return query.ToList();
        }

        public OperationResult ValidateDraft(
            Supplier? selectedSupplier,
            IEnumerable<ImportReceiptDetail>? draftDetails)
        {
            if (selectedSupplier == null)
            {
                return OperationResult.Failure("Vui lòng chọn Nhà cung cấp hợp lệ cho phiếu nhập hàng.");
            }

            var storedSupplier = _dataStore.GetSupplierById(selectedSupplier.MaNCC);
            if (storedSupplier == null)
            {
                return OperationResult.Failure("Nhà cung cấp được chọn không tồn tại trong hệ thống.");
            }

            if (!storedSupplier.IsActive)
            {
                return OperationResult.Failure(
                    $"Nhà cung cấp \"{storedSupplier.TenNCC}\" hiện đã ngừng hợp tác. Vui lòng chọn nhà cung cấp đang hoạt động.");
            }

            var lines = draftDetails?.ToList() ?? new List<ImportReceiptDetail>();
            if (lines.Count == 0)
            {
                return OperationResult.Failure("Danh sách sách nhập đang trống. Vui lòng thêm ít nhất 1 đầu sách vào phiếu nhập.");
            }

            var duplicateCheck = lines.GroupBy(l => l.MaSach).FirstOrDefault(g => g.Count() > 1);
            if (duplicateCheck != null)
            {
                return OperationResult.Failure($"Phát hiện dòng sách trùng lặp (Mã #{duplicateCheck.Key}) trong phiếu nhập.");
            }

            foreach (var line in lines)
            {
                var book = _dataStore.GetBookById(line.MaSach);
                if (book == null)
                {
                    return OperationResult.Failure($"Sách \"{line.TenSach}\" (Mã #{line.MaSach}) không tồn tại trong hệ thống.");
                }

                if (line.SoLuongNhap < 1)
                {
                    return OperationResult.Failure(
                        $"Số lượng nhập của sách \"{book.TenSach}\" (Mã #{book.MaSach}) phải là số nguyên dương (>= 1).");
                }

                if (line.DonGiaNhap <= 0m)
                {
                    return OperationResult.Failure(
                        $"Đơn giá nhập của sách \"{book.TenSach}\" (Mã #{book.MaSach}) phải lớn hơn 0 ₫.");
                }
            }

            return OperationResult.Success();
        }

        /// <summary>
        /// Atomically validates and confirms an import receipt.
        /// CRITICAL RULE: Book inventory is increased ONLY after all validations succeed.
        /// If any check fails, zero stock is modified and no receipt is added.
        /// </summary>
        public OperationResult<ImportReceipt> ConfirmImportReceipt(
            Supplier? selectedSupplier,
            IEnumerable<ImportReceiptDetail>? draftDetails,
            User? creatorManager,
            string? ghiChu = null)
        {
            // 1. Validate entire draft atomically BEFORE touching any Book stock
            var validation = ValidateDraft(selectedSupplier, draftDetails);
            if (!validation.IsSuccess)
            {
                return OperationResult<ImportReceipt>.Failure(validation.Message);
            }

            var storedSupplier = _dataStore.GetSupplierById(selectedSupplier!.MaNCC)!;
            var lines = draftDetails!.ToList();

            // 2. Prepare atomic book references and immutable detail snapshots
            var stockUpdates = new List<(Book TargetBook, ImportReceiptDetail SnapshotLine)>(lines.Count);
            decimal grandTotal = 0m;

            foreach (var draftLine in lines)
            {
                var targetBook = _dataStore.GetBookById(draftLine.MaSach);
                if (targetBook == null || draftLine.SoLuongNhap < 1 || draftLine.DonGiaNhap <= 0m)
                {
                    return OperationResult<ImportReceipt>.Failure(
                        "Dữ liệu chi tiết phiếu nhập không hợp lệ. Tồn kho được giữ nguyên.");
                }

                var snapshot = new ImportReceiptDetail
                {
                    MaSach = targetBook.MaSach,
                    TenSach = targetBook.TenSach,
                    TacGia = targetBook.TacGia,
                    CurrentStock = targetBook.SoLuongTon,
                    SoLuongNhap = draftLine.SoLuongNhap,
                    DonGiaNhap = draftLine.DonGiaNhap
                };

                grandTotal += snapshot.ThanhTien;
                stockUpdates.Add((targetBook, snapshot));
            }

            // 3. Apply stock increases to shared Book instances
            int totalQuantityImported = 0;
            foreach (var (targetBook, snapshotLine) in stockUpdates)
            {
                targetBook.SoLuongTon += snapshotLine.SoLuongNhap;
                totalQuantityImported += snapshotLine.SoLuongNhap;
            }

            // 4. Create confirmed ImportReceipt with immutable supplier & line snapshots
            var confirmedReceipt = new ImportReceipt
            {
                MaPN = _dataStore.GenerateNextImportReceiptId(),
                MaNCC = storedSupplier.MaNCC,
                TenNCC = storedSupplier.TenNCC,
                SdtNCC = storedSupplier.SDT,
                EmailNCC = storedSupplier.Email,
                DiaChiNCC = storedSupplier.DiaChi,
                NgayLap = DateTime.Now,
                TongTien = grandTotal,
                TrangThai = ImportReceiptStatus.Confirmed,
                NguoiLapMaND = creatorManager?.MaND ?? 5,
                NguoiLapHoTen = creatorManager?.HoTen ?? "Quản lý",
                GhiChu = string.IsNullOrWhiteSpace(ghiChu)
                    ? "Nhập hàng bổ sung tồn kho"
                    : ghiChu.Trim()
            };

            foreach (var (_, snapshotLine) in stockUpdates)
            {
                confirmedReceipt.Details.Add(snapshotLine);
            }

            // 5. Add to shared MockDataStore and notify all observers
            _dataStore.ImportReceipts.Insert(0, confirmedReceipt);
            ImportReceiptConfirmed?.Invoke(this, confirmedReceipt);
            _dataStore.NotifyDataChanged();

            string successMsg =
                $"Xác nhận lập phiếu nhập hàng thành công!\n\n" +
                $"• Mã phiếu nhập: {confirmedReceipt.ReceiptCode}\n" +
                $"• Nhà cung cấp: {confirmedReceipt.TenNCC}\n" +
                $"• Số đầu sách nhập: {confirmedReceipt.DistinctBookCount} đầu sách ({totalQuantityImported} cuốn)\n" +
                $"• Tổng tiền phiếu nhập: {confirmedReceipt.FormattedTongTien}\n" +
                $"• Trạng thái: {confirmedReceipt.TrangThaiLabel}\n\n" +
                $"Tồn kho của các đầu sách tương ứng đã được cộng thêm đồng bộ trên toàn hệ thống.";

            return OperationResult<ImportReceipt>.Success(confirmedReceipt, successMsg);
        }
    }
}
