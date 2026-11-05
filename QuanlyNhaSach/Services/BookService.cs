using System;
using System.Collections.Generic;
using System.Linq;
using QuanlyNhaSach.Models;
using Microsoft.EntityFrameworkCore;

namespace QuanlyNhaSach.Services
{
    public class BookService
    {
        public const string AllStockFilter = "Tất cả";
        public const string NormalStockFilter = "Đủ hàng (>= 10)";
        public const string LowStockFilter = "Sắp hết (< 10)";
        public const string OutOfStockFilter = "Hết hàng (0)";

        private readonly object _cacheLock = new();
        private List<SACH>? _booksCache;
        private List<NHACUNGCAP>? _suppliersCache;

        public event EventHandler? BooksChanged;

        public string LastErrorMessage { get; private set; } = string.Empty;

        public void InvalidateCache()
        {
            lock (_cacheLock)
            {
                _booksCache = null;
                _suppliersCache = null;
            }
        }

        public IReadOnlyList<NHACUNGCAP> GetAvailableSuppliers()
        {
            lock (_cacheLock)
            {
                if (_suppliersCache != null)
                {
                    return _suppliersCache;
                }

                try
                {
                    using var db = new QuanlyNhaSachContext();
                    _suppliersCache = db.NHACUNGCAPs
                        .AsNoTracking()
                        .OrderBy(n => n.MaNCC)
                        .ToList();
                    return _suppliersCache;
                }
                catch (Exception ex)
                {
                    LastErrorMessage = ex.Message;
                    return Array.Empty<NHACUNGCAP>();
                }
            }
        }

        public IReadOnlyList<SACH> GetAllBooks()
        {
            lock (_cacheLock)
            {
                if (_booksCache != null)
                {
                    return _booksCache;
                }

                try
                {
                    using var db = new QuanlyNhaSachContext();
                    LastErrorMessage = string.Empty;
                    _booksCache = db.SACHs
                        .AsNoTracking()
                        .Where(s => s.Daxoa == false || s.Daxoa == null)
                        .OrderBy(s => s.MaSach)
                        .ToList();
                    return _booksCache;
                }
                catch (Exception ex)
                {
                    LastErrorMessage = ex.Message;
                    return Array.Empty<SACH>();
                }
            }
        }

        public IReadOnlyList<SACH> GetLowStockBooks()
        {
            return GetAllBooks()
                .Where(s => s.Soluongton < 10 && s.Soluongton > 0)
                .OrderBy(s => s.Soluongton)
                .ThenBy(s => s.MaSach)
                .ToList();
        }

        public int GetLowStockBookCount()
        {
            return GetAllBooks()
                .Count(s => s.Soluongton < 10 && s.Soluongton > 0);
        }

        public SACH? GetBookById(string maSach)
        {
            if (string.IsNullOrWhiteSpace(maSach))
            {
                return null;
            }

            string key = maSach.Trim();
            return GetAllBooks()
                .FirstOrDefault(s => string.Equals(s.MaSach?.Trim(), key, StringComparison.OrdinalIgnoreCase));
        }

        public IReadOnlyList<SACH> SearchAndFilterBooks(string? keyword, string? stockFilter = null)
        {
            IEnumerable<SACH> query = GetAllBooks();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string kw = keyword.Trim();
                query = query.Where(s =>
                    (s.Tensach != null && s.Tensach.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
                    (s.Tacgia != null && s.Tacgia.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
                    (s.MaSach != null && s.MaSach.Contains(kw, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.IsNullOrWhiteSpace(stockFilter) && stockFilter != AllStockFilter)
            {
                if (stockFilter == NormalStockFilter)
                    query = query.Where(s => s.Soluongton >= 10);
                else if (stockFilter == LowStockFilter)
                    query = query.Where(s => s.Soluongton < 10 && s.Soluongton > 0);
                else if (stockFilter == OutOfStockFilter)
                    query = query.Where(s => s.Soluongton == 0 || s.Soluongton == null);
            }

            return query.OrderBy(s => s.MaSach).ToList();
        }

        public OperationResult<SACH> AddBook(
            string maSachMoi, string tenSach, string tacGia, string theLoai,
            string nxb, string maNCC, decimal giaBan, byte soLuongTon)
        {
            var validationError = ValidateBookFields(maSachMoi, tenSach, tacGia, theLoai, nxb, maNCC, giaBan);
            if (validationError != null)
            {
                return OperationResult<SACH>.Failure(validationError);
            }

            try
            {
                using var db = new QuanlyNhaSachContext();

                bool isIdExist = db.SACHs.AsNoTracking().Any(s => s.MaSach == maSachMoi);
                if (isIdExist) return OperationResult<SACH>.Failure($"Mã sách \"{maSachMoi}\" đã bị trùng trong hệ thống!");

                bool isDuplicate = db.SACHs.AsNoTracking().Any(s => (s.Daxoa == false || s.Daxoa == null) && s.Tensach == tenSach && s.Tacgia == tacGia);
                if (isDuplicate) return OperationResult<SACH>.Failure("Sách có cùng tên và tác giả này đã tồn tại trong hệ thống!");

                bool supplierExists = db.NHACUNGCAPs.AsNoTracking().Any(n => n.MaNCC == maNCC);
                if (!supplierExists)
                {
                    var existingCodes = db.NHACUNGCAPs.AsNoTracking().Select(n => n.MaNCC).Take(10).ToList();
                    string hint = existingCodes.Count > 0
                        ? $"Mã NCC hợp lệ hiện có: {string.Join(", ", existingCodes)}."
                        : "Hiện nhà cung cấp sách này chưa có trong hệ thống. Vui lòng thêm thông tin nhà cung cấp trước khi thêm sách!";
                    return OperationResult<SACH>.Failure($"Mã nhà cung cấp \"{maNCC}\" này chưa tồn tại trong hệ thống.{hint}");
                }

                var newBook = new SACH
                {
                    MaSach = maSachMoi,
                    Tensach = tenSach,
                    Tacgia = tacGia,
                    Theloai = theLoai,
                    NXB = nxb,
                    MaNCC = maNCC,
                    Giaban = giaBan,
                    Soluongton = soLuongTon,
                    Daxoa = false
                };

                db.SACHs.Add(newBook);
                db.SaveChanges();

                InvalidateCache();
                NotifyChanged();
                return OperationResult<SACH>.Success(newBook, "Đã thêm sách mới thành công.");
            }
            catch (DbUpdateException dbEx)
            {
                string detail = dbEx.InnerException?.Message ?? dbEx.Message;
                return OperationResult<SACH>.Failure($"Không thể lưu sách vào hệ thống: {detail}");
            }
            catch (Exception ex)
            {
                return OperationResult<SACH>.Failure($"Lỗi kết nối: {ex.Message}");
            }
        }

        public OperationResult<SACH> UpdateBook(
            string maSach, string tenSach, string tacGia, string theLoai,
            string nxb, string maNCC, decimal giaBan, byte soLuongTon)
        {
            var validationError = ValidateBookFields(maSach, tenSach, tacGia, theLoai, nxb, maNCC, giaBan);
            if (validationError != null)
            {
                return OperationResult<SACH>.Failure(validationError);
            }

            try
            {
                using var db = new QuanlyNhaSachContext();

                var existingBook = db.SACHs.FirstOrDefault(s => s.MaSach == maSach);
                if (existingBook == null) return OperationResult<SACH>.Failure("Không tìm thấy sách.");

                bool isDuplicate = db.SACHs.AsNoTracking().Any(b => b.MaSach != maSach && (b.Daxoa == false || b.Daxoa == null) && b.Tensach == tenSach && b.Tacgia == tacGia);
                if (isDuplicate) return OperationResult<SACH>.Failure("Tên sách và tác giả này đã tồn tại ở mã khác.");

                bool supplierExists = db.NHACUNGCAPs.AsNoTracking().Any(n => n.MaNCC == maNCC);
                if (!supplierExists)
                {
                    var existingCodes = db.NHACUNGCAPs.AsNoTracking().Select(n => n.MaNCC).Take(10).ToList();
                    string hint = existingCodes.Count > 0
                        ? $" Mã NCC hợp lệ hiện có: {string.Join(", ", existingCodes)}."
                        : " Hệ thống chưa có dữ liệu nhà cung cấp sách cần cập nhật.";
                    return OperationResult<SACH>.Failure($"Mã nhà cung cấp \"{maNCC}\" không tồn tại trong hệ thống.{hint}");
                }

                existingBook.Tensach = tenSach;
                existingBook.Tacgia = tacGia;
                existingBook.Theloai = theLoai;
                existingBook.NXB = nxb;
                existingBook.MaNCC = maNCC;
                existingBook.Giaban = giaBan;
                existingBook.Soluongton = soLuongTon;

                db.SaveChanges();

                InvalidateCache();
                NotifyChanged();
                return OperationResult<SACH>.Success(existingBook, "Cập nhật thành công.");
            }
            catch (DbUpdateException dbEx)
            {
                string detail = dbEx.InnerException?.Message ?? dbEx.Message;
                return OperationResult<SACH>.Failure($"Không thể cập nhật sách vào hệ thống: {detail}");
            }
            catch (Exception ex)
            {
                return OperationResult<SACH>.Failure($"Lỗi kết nối: {ex.Message}");
            }
        }

        public OperationResult DeleteBook(string maSach)
        {
            try
            {
                using var db = new QuanlyNhaSachContext();

                var book = db.SACHs
                    .Include(s => s.CHITIETDONHANGs)
                    .Include(s => s.CHITIETPHIEUNHAPs)
                    .Include(s => s.CHITIETGIOHANGs)
                    .FirstOrDefault(s => s.MaSach == maSach);

                if (book == null) return OperationResult.Failure("Không tìm thấy sách.");

                bool hasHistory = book.CHITIETDONHANGs.Any() ||
                                  book.CHITIETPHIEUNHAPs.Any() ||
                                  book.CHITIETGIOHANGs.Any();

                if (hasHistory)
                {
                    book.Daxoa = true;
                }
                else
                {
                    db.SACHs.Remove(book);
                }

                db.SaveChanges();
                InvalidateCache();
                NotifyChanged();
                return OperationResult.Success("Đã xóa thành công!");
            }
            catch (DbUpdateException dbEx)
            {
                string detail = dbEx.InnerException?.Message ?? dbEx.Message;
                return OperationResult.Failure($"Không thể xóa sách: {detail}");
            }
            catch (Exception ex)
            {
                return OperationResult.Failure($"Lỗi kết nối: {ex.Message}");
            }
        }

        public List<string?> GetBookSuggestions(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return new List<string?>();

            string kw = keyword.Trim();
            return GetAllBooks()
                .Where(s => (s.Tensach != null && s.Tensach.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
                            (s.MaSach != null && s.MaSach.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
                            (s.Tacgia != null && s.Tacgia.Contains(kw, StringComparison.OrdinalIgnoreCase)))
                .Select(s => s.Tensach)
                .Distinct()
                .Take(10)
                .ToList();
        }

        private static string? ValidateBookFields(
            string maSach, string tenSach, string tacGia, string theLoai,
            string nxb, string maNCC, decimal giaBan)
        {
            if (string.IsNullOrWhiteSpace(maSach))
                return "Vui lòng nhập mã sách.";
            if (maSach.Length > 10)
                return "Mã sách không được vượt quá 10 ký tự.";

            if (string.IsNullOrWhiteSpace(tenSach))
                return "Vui lòng nhập tên đầu sách.";
            if (tenSach.Length > 100)
                return "Tên sách không được vượt quá 100 ký tự.";

            if (string.IsNullOrWhiteSpace(tacGia))
                return "Vui lòng nhập tên tác giả.";
            if (tacGia.Length > 100)
                return "Tên tác giả không được vượt quá 100 ký tự.";

            if (theLoai != null && theLoai.Length > 100)
                return "Thể loại không được vượt quá 100 ký tự.";

            if (string.IsNullOrWhiteSpace(nxb))
                return "Vui lòng nhập nhà xuất bản.";
            if (nxb.Length > 100)
                return "Nhà xuất bản không được vượt quá 100 ký tự.";

            if (string.IsNullOrWhiteSpace(maNCC))
                return "Vui lòng nhập mã nhà cung cấp (MaNCC).";
            if (maNCC.Length > 10)
                return "Mã nhà cung cấp không được vượt quá 10 ký tự.";

            if (giaBan <= 0m || giaBan > 99_999_999.99m)
                return "Giá bán trong khoảng 1-100 triệu.";

            return null;
        }

        private void NotifyChanged()
        {
            BooksChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}