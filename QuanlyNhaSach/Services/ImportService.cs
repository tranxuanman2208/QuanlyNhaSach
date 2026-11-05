using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using QuanlyNhaSach.Models;

namespace QuanlyNhaSach.Services
{
    public class ImportService
    {
        private const decimal MaxSqlDecimal10_2 = 99_999_999.99m;

        private readonly object _cacheLock = new();
        private List<PHIEUNHAP>? _receiptsCache;

        public event EventHandler<PHIEUNHAP>? ImportReceiptConfirmed;

        public ImportService()
        {
        }

        public void InvalidateCache()
        {
            lock (_cacheLock)
            {
                _receiptsCache = null;
            }
        }

        public int PeekNextReceiptId()
        {
            var cached = GetConfirmedReceipts();
            if (cached.Count > 0)
            {
                return cached.Max(p => p.MaPN) + 1;
            }

            try
            {
                using var db = new QuanlyNhaSachContext();
                int maxId = db.PHIEUNHAPs.AsNoTracking().Select(p => (int?)p.MaPN).Max() ?? 0;
                return maxId + 1;
            }
            catch
            {
                return 1;
            }
        }

        public IReadOnlyList<PHIEUNHAP> GetConfirmedReceipts()
        {
            lock (_cacheLock)
            {
                if (_receiptsCache != null)
                {
                    return _receiptsCache;
                }

                try
                {
                    using var db = new QuanlyNhaSachContext();
                    _receiptsCache = db.PHIEUNHAPs
                        .AsNoTracking()
                        .Include(p => p.MaNCCNavigation)
                        .Include(p => p.MaQlyNavigation)
                        .Include(p => p.CHITIETPHIEUNHAPs)
                            .ThenInclude(d => d.MaSachNavigation)
                        .OrderByDescending(p => p.NgayNhap)
                        .ThenByDescending(p => p.MaPN)
                        .ToList();

                    return _receiptsCache;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Lỗi khi tải lịch sử phiếu nhập: {ex.Message}");
                    return Array.Empty<PHIEUNHAP>();
                }
            }
        }

        public IReadOnlyList<PHIEUNHAP> SearchConfirmedReceipts(
            string? keyword,
            string? supplierId = null)
        {
            IEnumerable<PHIEUNHAP> query = GetConfirmedReceipts();

            if (!string.IsNullOrWhiteSpace(supplierId))
            {
                string supKey = supplierId.Trim();
                query = query.Where(r => string.Equals(r.MaNCC?.Trim(), supKey, StringComparison.OrdinalIgnoreCase));
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
                    (r.CHITIETPHIEUNHAPs != null && r.CHITIETPHIEUNHAPs.Any(d =>
                        d.TenSach.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                        d.MaSach.Contains(kw, StringComparison.OrdinalIgnoreCase))));
            }

            return query.ToList();
        }

        public OperationResult ValidateDraft(
            NHACUNGCAP? selectedSupplier,
            IEnumerable<CHITIETPHIEUNHAP>? draftDetails)
        {
            if (selectedSupplier == null || string.IsNullOrWhiteSpace(selectedSupplier.MaNCC))
            {
                return OperationResult.Failure("Vui lòng chọn Nhà cung cấp hợp lệ cho phiếu nhập hàng.");
            }

            if (!selectedSupplier.IsActive)
            {
                return OperationResult.Failure(
                    $"Nhà cung cấp \"{selectedSupplier.TenNCC}\" hiện đã ngừng hợp tác. Vui lòng chọn nhà cung cấp đang hoạt động.");
            }

            var lines = draftDetails?.ToList() ?? new List<CHITIETPHIEUNHAP>();
            if (lines.Count == 0)
            {
                return OperationResult.Failure("Danh sách sách nhập đang trống. Vui lòng thêm ít nhất 1 sách vào phiếu nhập.");
            }

            var duplicateCheck = lines
                .GroupBy(l => l.MaSach?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(g => g.Count() > 1);
            if (duplicateCheck != null)
            {
                return OperationResult.Failure($"Mã sách #{duplicateCheck.Key} bị trùng trong phiếu nhập.");
            }

            if (lines.Any(l => string.IsNullOrWhiteSpace(l.MaSach)))
            {
                return OperationResult.Failure("Mã sách không hợp lệ.");
            }

            try
            {
                using var db = new QuanlyNhaSachContext();

                string supKey = selectedSupplier.MaNCC.Trim();
                bool supplierExists = db.NHACUNGCAPs.AsNoTracking().Any(s => s.MaNCC == supKey);
                if (!supplierExists)
                {
                    return OperationResult.Failure("Nhà cung cấp được chọn không tồn tại trong hệ thống.");
                }

                // Tải toàn bộ sách trong phiếu nhập chỉ bằng 1 câu truy vấn duy nhất (tránh lỗi N+1)
                var bookKeys = lines.Select(l => l.MaSach.Trim()).Distinct().ToList();
                var booksDict = db.SACHs
                    .AsNoTracking()
                    .Where(b => bookKeys.Contains(b.MaSach) && (b.Daxoa == null || b.Daxoa == false))
                    .ToDictionary(b => b.MaSach.Trim(), b => b, StringComparer.OrdinalIgnoreCase);

                decimal grandTotal = 0m;

                foreach (var line in lines)
                {
                    string bookKey = line.MaSach.Trim();
                    if (!booksDict.TryGetValue(bookKey, out var book))
                    {
                        return OperationResult.Failure($"Sách \"{line.TenSach}\" không tồn tại.");
                    }

                    if (line.SoLuongNhap < 1)
                    {
                        return OperationResult.Failure(
                            $"Số lượng nhập của sách \"{book.Tensach}\" phải lớn hơn hoặc bằng 1.");
                    }

                    int currentStock = book.Soluongton ?? 0;
                    if (currentStock + line.SoLuongNhap > byte.MaxValue)
                    {
                        return OperationResult.Failure(
                            $"Số lượng sách \"{book.Tensach}\" ({currentStock} + {line.SoLuongNhap} = {currentStock + line.SoLuongNhap}) vượt quá giới hạn lưu trữ tối đa.");
                    }

                    if (line.DonGiaNhap <= 0m)
                    {
                        return OperationResult.Failure(
                            $"Đơn giá nhập của sách \"{book.Tensach}\" phải lớn hơn 0 ₫.");
                    }

                    if (line.ThanhTien > MaxSqlDecimal10_2)
                    {
                        return OperationResult.Failure(
                            $"Thành tiền của sách \"{book.Tensach}\" vượt quá giới hạn tối đa.");
                    }

                    grandTotal += line.ThanhTien;
                }

                if (grandTotal > MaxSqlDecimal10_2)
                {
                    return OperationResult.Failure(
                        $"Tổng tiền phiếu nhập {grandTotal:0} ₫ vượt quá giới hạn lưu trữ của cơ sở dữ liệu.");
                }

                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                return OperationResult.Failure(
                    $"Không thể kiểm tra dữ liệu phiếu nhập với cơ sở dữ liệu: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        public OperationResult<PHIEUNHAP> ConfirmImportReceipt(
            NHACUNGCAP? selectedSupplier,
            IEnumerable<CHITIETPHIEUNHAP>? draftDetails,
            NGUOIDUNG? creatorManager,
            string? ghiChu = null)
        {
            var validation = ValidateDraft(selectedSupplier, draftDetails);
            if (!validation.IsSuccess)
            {
                return OperationResult<PHIEUNHAP>.Failure(validation.Message);
            }

            var lines = draftDetails!.ToList();
            string supKey = selectedSupplier!.MaNCC.Trim();

            try
            {
                using var db = new QuanlyNhaSachContext();
                using var transaction = db.Database.BeginTransaction();

                try
                {
                    var storedSupplier = db.NHACUNGCAPs.FirstOrDefault(s => s.MaNCC == supKey);
                    if (storedSupplier == null)
                    {
                        transaction.Rollback();
                        return OperationResult<PHIEUNHAP>.Failure("Nhà cung cấp không tồn tại trong hệ thống.");
                    }

                    string? resolvedMaQly = null;
                    string managerDisplayName = "Quản lý";

                    if (creatorManager is NGUOIDUNG ndUser)
                    {
                        managerDisplayName = !string.IsNullOrWhiteSpace(ndUser.Hoten) ? ndUser.Hoten : ndUser.MaND;
                        if (!string.IsNullOrWhiteSpace(ndUser.MaND) &&
                            db.NGUOIDUNGs.AsNoTracking().Any(u => u.MaND == ndUser.MaND))
                        {
                            resolvedMaQly = ndUser.MaND;
                        }
                    }

                    if (resolvedMaQly == null)
                    {
                        var fallbackManager = db.NGUOIDUNGs.AsNoTracking().FirstOrDefault(u =>
                            u.Vaitro == "Quản lý" || u.MaND.StartsWith("QL"));
                        resolvedMaQly = fallbackManager?.MaND;
                        if (fallbackManager != null && !string.IsNullOrWhiteSpace(fallbackManager.Hoten) && creatorManager == null)
                        {
                            managerDisplayName = fallbackManager.Hoten;
                        }
                    }

                    decimal grandTotal = lines.Sum(l => l.ThanhTien);
                    DateTime now = DateTime.Now;

                    var receipt = new PHIEUNHAP
                    {
                        MaNCC = storedSupplier.MaNCC,
                        MaQly = resolvedMaQly,
                        NgayNhap = DateOnly.FromDateTime(now),
                        Dathanhtoan = true,
                        Tongtien = grandTotal,
                        NgayLap = now,
                        TenNCC = storedSupplier.TenNCC ?? storedSupplier.MaNCC,
                        SdtNCC = storedSupplier.SDT ?? string.Empty,
                        EmailNCC = storedSupplier.Email ?? string.Empty,
                        DiaChiNCC = storedSupplier.Diachi ?? string.Empty,
                        NguoiLapHoTen = managerDisplayName,
                        GhiChu = string.IsNullOrWhiteSpace(ghiChu) ? "Nhập bổ sung sách vào kho" : ghiChu.Trim()
                    };

                    db.PHIEUNHAPs.Add(receipt);
                    db.SaveChanges();

                    // Lấy toàn bộ sách cần cập nhật tồn kho trong 1 lần truy vấn
                    var bookKeys = lines.Select(l => l.MaSach.Trim()).Distinct().ToList();
                    var trackedBooks = db.SACHs
                        .Where(b => bookKeys.Contains(b.MaSach) && (b.Daxoa == null || b.Daxoa == false))
                        .ToDictionary(b => b.MaSach.Trim(), b => b, StringComparer.OrdinalIgnoreCase);

                    int totalUnitsImported = 0;

                    foreach (var line in lines)
                    {
                        string bookKey = line.MaSach.Trim();
                        if (!trackedBooks.TryGetValue(bookKey, out var book))
                        {
                            transaction.Rollback();
                            return OperationResult<PHIEUNHAP>.Failure(
                                $"Sách \"{line.TenSach}\" không tồn tại trong hệ thống.");
                        }

                        int currentStock = book.Soluongton ?? 0;
                        int newStock = currentStock + line.SoLuongNhap;
                        if (newStock > byte.MaxValue)
                        {
                            transaction.Rollback();
                            return OperationResult<PHIEUNHAP>.Failure(
                                $"Tồn kho của sách \"{book.Tensach}\" vượt quá giới hạn tối đa.");
                        }

                        book.Soluongton = (byte)newStock;
                        totalUnitsImported += line.SoLuongNhap;

                        var detailEntity = new CHITIETPHIEUNHAP
                        {
                            MaPN = receipt.MaPN,
                            MaSach = book.MaSach,
                            Soluongnhap = (byte)line.SoLuongNhap,
                            Tong = line.ThanhTien,
                            TenSach = book.Tensach ?? line.TenSach,
                            TacGia = book.Tacgia ?? line.TacGia,
                            CurrentStock = newStock
                        };

                        db.CHITIETPHIEUNHAPs.Add(detailEntity);
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    InvalidateCache();
                    receipt.MaNCCNavigation = storedSupplier;
                    ImportReceiptConfirmed?.Invoke(this, receipt);

                    return OperationResult<PHIEUNHAP>.Success(
                        receipt,
                        $"Lập phiếu nhập {receipt.ReceiptCode} thành công. Tồn kho đã được cập nhật.");
                }
                catch (Exception innerEx)
                {
                    transaction.Rollback();
                    return OperationResult<PHIEUNHAP>.Failure(
                        $"Lỗi giao dịch khi lưu phiếu nhập hàng: {innerEx.InnerException?.Message ?? innerEx.Message}");
                }
            }
            catch (Exception ex)
            {
                return OperationResult<PHIEUNHAP>.Failure(
                    $"Không thể kết nối để lập phiếu nhập: {ex.InnerException?.Message ?? ex.Message}");
            }
        }
    }
}