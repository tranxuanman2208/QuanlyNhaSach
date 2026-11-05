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

        public event EventHandler? BooksChanged;

        public string LastErrorMessage { get; private set; } = string.Empty;

        public IReadOnlyList<NHACUNGCAP> GetAvailableSuppliers()
        {
            try
            {
                using (var db = new QuanlyNhaSachContext())
                {
                    return db.NHACUNGCAPs
                             .AsNoTracking()
                             .OrderBy(n => n.MaNCC)
                             .ToList();
                }
            }
            catch (Exception ex)
            {
                LastErrorMessage = ex.Message;
                return Array.Empty<NHACUNGCAP>();
            }
        }

        public IReadOnlyList<SACH> GetAllBooks()
        {
            try
            {
                using (var db = new QuanlyNhaSachContext())
                {
                    LastErrorMessage = string.Empty;
                    return db.SACHs
                             .AsNoTracking()
                             .Where(s => s.Daxoa == false || s.Daxoa == null)
                             .OrderBy(s => s.MaSach)
                             .ToList();
                }
            }
            catch (Exception ex)
            {
                LastErrorMessage = ex.Message;
                return Array.Empty<SACH>();
            }
        }

        public IReadOnlyList<SACH> GetLowStockBooks()
        {
            try
            {
                using (var db = new QuanlyNhaSachContext())
                {
                    return db.SACHs
                             .AsNoTracking()
                             .Where(s => s.Soluongton < 10 && s.Soluongton > 0 && (s.Daxoa == false || s.Daxoa == null))
                             .OrderBy(s => s.Soluongton).ThenBy(s => s.MaSach)
                             .ToList();
                }
            }
            catch (Exception ex)
            {
                LastErrorMessage = ex.Message;
                return Array.Empty<SACH>();
            }
        }

        public int GetLowStockBookCount()
        {
            try
            {
                using (var db = new QuanlyNhaSachContext())
                {
                    return db.SACHs
                             .AsNoTracking()
                             .Count(s => s.Soluongton < 10 && s.Soluongton > 0 && (s.Daxoa == false || s.Daxoa == null));
                }
            }
            catch (Exception ex)
            {
                LastErrorMessage = ex.Message;
                return 0;
            }
        }

        public SACH? GetBookById(string maSach)
        {
            if (string.IsNullOrWhiteSpace(maSach))
            {
                return null;
            }

            try
            {
                using (var db = new QuanlyNhaSachContext())
                {
                    // Tìm đúng mã sách
                    return db.SACHs
                             .AsNoTracking()
                             .FirstOrDefault(s => s.MaSach == maSach && (s.Daxoa == false || s.Daxoa == null));
                }
            }
            catch (Exception ex)
            {
                LastErrorMessage = ex.Message;
                return null;
            }
        }

        public IReadOnlyList<SACH> SearchAndFilterBooks(string? keyword, string? stockFilter = null)
        {
            try
            {
                using (var db = new QuanlyNhaSachContext())
                {
                    LastErrorMessage = string.Empty;

                    // Chặn sách đã xóa
                    var query = db.SACHs
                                  .AsNoTracking()
                                  .Where(s => s.Daxoa == false || s.Daxoa == null);

                    // Lọc theo từ khóa
                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        string kw = keyword.Trim();
                        query = query.Where(s =>
                            (s.Tensach != null && s.Tensach.Contains(kw)) ||
                            (s.Tacgia != null && s.Tacgia.Contains(kw)) ||
                            s.MaSach.Contains(kw));
                    }

                    // Lọc theo tồn kho
                    if (!string.IsNullOrWhiteSpace(stockFilter) && stockFilter != AllStockFilter)
                    {
                        if (stockFilter == NormalStockFilter)
                            query = query.Where(s => s.Soluongton >= 10);
                        else if (stockFilter == LowStockFilter)
                            query = query.Where(s => s.Soluongton < 10 && s.Soluongton > 0);
                        else if (stockFilter == OutOfStockFilter)
                            query = query.Where(s => s.Soluongton == 0 || s.Soluongton == null);
                    }

                    // Sắp xếp và thực thi
                    return query.OrderBy(s => s.MaSach).ToList();
                }
            }
            catch (Exception ex)
            {
                LastErrorMessage = ex.Message;
                return Array.Empty<SACH>();
            }
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
                using (var db = new QuanlyNhaSachContext())
                {
                    bool isIdExist = db.SACHs.Any(s => s.MaSach == maSachMoi);
                    if (isIdExist) return OperationResult<SACH>.Failure($"Mã sách \"{maSachMoi}\" đã bị trùng trong hệ thống!");

                    bool isDuplicate = db.SACHs.Any(s => (s.Daxoa == false || s.Daxoa == null) && s.Tensach == tenSach && s.Tacgia == tacGia);
                    if (isDuplicate) return OperationResult<SACH>.Failure("Sách có cùng tên và tác giả này đã tồn tại trong hệ thống!");

                    bool supplierExists = db.NHACUNGCAPs.AsNoTracking().Any(n => n.MaNCC == maNCC);
                    if (!supplierExists)
                    {
                        var existingCodes = db.NHACUNGCAPs.AsNoTracking().Select(n => n.MaNCC).Take(10).ToList();
                        string hint = existingCodes.Count > 0
                            ? $" Mã NCC hợp lệ hiện có: {string.Join(", ", existingCodes)}."
                            : " Hiện bảng NHACUNGCAP chưa có dữ liệu nhà cung cấp. Vui lòng thêm nhà cung cấp vào bảng NHACUNGCAP trong SQL Server trước khi thêm sách.";
                        return OperationResult<SACH>.Failure($"Mã nhà cung cấp \"{maNCC}\" không tồn tại trong bảng NHACUNGCAP (ràng buộc khóa ngoại fk_MaNCC).{hint}");
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
                    NotifyChanged();
                    return OperationResult<SACH>.Success(newBook, "Đã thêm sách mới thành công.");
                }
            }
            catch (DbUpdateException dbEx)
            {
                string detail = dbEx.InnerException?.Message ?? dbEx.Message;
                return OperationResult<SACH>.Failure($"Không thể lưu sách vào cơ sở dữ liệu (lỗi ràng buộc SQL Server): {detail}");
            }
            catch (Exception ex)
            {
                return OperationResult<SACH>.Failure($"Lỗi kết nối hoặc truy vấn SQL Server: {ex.Message}");
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
                using (var db = new QuanlyNhaSachContext())
                {
                    var existingBook = db.SACHs.FirstOrDefault(s => s.MaSach == maSach);
                    if (existingBook == null) return OperationResult<SACH>.Failure("Không tìm thấy sách.");

                    bool isDuplicate = db.SACHs.Any(b => b.MaSach != maSach && (b.Daxoa == false || b.Daxoa == null) && b.Tensach == tenSach && b.Tacgia == tacGia);
                    if (isDuplicate) return OperationResult<SACH>.Failure("Tên sách và tác giả này đã tồn tại ở mã khác.");

                    bool supplierExists = db.NHACUNGCAPs.AsNoTracking().Any(n => n.MaNCC == maNCC);
                    if (!supplierExists)
                    {
                        var existingCodes = db.NHACUNGCAPs.AsNoTracking().Select(n => n.MaNCC).Take(10).ToList();
                        string hint = existingCodes.Count > 0
                            ? $" Mã NCC hợp lệ hiện có: {string.Join(", ", existingCodes)}."
                            : " Hiện bảng NHACUNGCAP chưa có dữ liệu nhà cung cấp.";
                        return OperationResult<SACH>.Failure($"Mã nhà cung cấp \"{maNCC}\" không tồn tại trong bảng NHACUNGCAP (ràng buộc khóa ngoại fk_MaNCC).{hint}");
                    }

                    existingBook.Tensach = tenSach;
                    existingBook.Tacgia = tacGia;
                    existingBook.Theloai = theLoai;
                    existingBook.NXB = nxb;
                    existingBook.MaNCC = maNCC;
                    existingBook.Giaban = giaBan;
                    existingBook.Soluongton = soLuongTon;

                    db.SaveChanges();
                    NotifyChanged();
                    return OperationResult<SACH>.Success(existingBook, "Cập nhật thành công.");
                }
            }
            catch (DbUpdateException dbEx)
            {
                string detail = dbEx.InnerException?.Message ?? dbEx.Message;
                return OperationResult<SACH>.Failure($"Không thể cập nhật sách vào cơ sở dữ liệu (lỗi ràng buộc SQL Server): {detail}");
            }
            catch (Exception ex)
            {
                return OperationResult<SACH>.Failure($"Lỗi kết nối hoặc truy vấn SQL Server: {ex.Message}");
            }
        }

        public OperationResult DeleteBook(string maSach)
        {
            try
            {
                using (var db = new QuanlyNhaSachContext())
                {
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
                        // Xóa mềm
                        book.Daxoa = true;
                        db.SaveChanges();
                        NotifyChanged();
                        return OperationResult.Success("Đã xóa thành công!");
                    }
                    else
                    {
                        // Xóa cứng
                        db.SACHs.Remove(book);
                        db.SaveChanges();
                        NotifyChanged();
                        return OperationResult.Success("Đã xóa thành công!");
                    }
                }
            }
            catch (DbUpdateException dbEx)
            {
                string detail = dbEx.InnerException?.Message ?? dbEx.Message;
                return OperationResult.Failure($"Không thể xóa sách do ràng buộc dữ liệu: {detail}");
            }
            catch (Exception ex)
            {
                return OperationResult.Failure($"Lỗi kết nối hoặc truy vấn SQL Server: {ex.Message}");
            }
        }

        public List<string?> GetBookSuggestions(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return new List<string?>();

            try
            {
                string kw = keyword.Trim();
                using (var db = new QuanlyNhaSachContext())
                {
                    return db.SACHs
                             .AsNoTracking()
                             .Where(s => (s.Daxoa == false || s.Daxoa == null)
                                         && ((s.Tensach != null && s.Tensach.Contains(kw))
                                             || s.MaSach.Contains(kw)
                                             || (s.Tacgia != null && s.Tacgia.Contains(kw))))
                             .Select(s => s.Tensach) // Chỉ lấy cột Tên sách
                             .Distinct()             // Loại bỏ các tên bị trùng
                             .Take(10)               // Chỉ lấy tối đa 10 gợi ý đầu tiên
                             .ToList();
                }
            }
            catch (Exception ex)
            {
                LastErrorMessage = ex.Message;
                return new List<string?>();
            }
        }

        private static string? ValidateBookFields(
            string maSach, string tenSach, string tacGia, string theLoai,
            string nxb, string maNCC, decimal giaBan)
        {
            if (string.IsNullOrWhiteSpace(maSach))
                return "Vui lòng nhập mã sách.";
            if (maSach.Length > 10)
                return "Mã sách không được vượt quá 10 ký tự (varchar(10)).";

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
                return "Mã nhà cung cấp không được vượt quá 10 ký tự (varchar(10)).";

            if (giaBan <= 0m || giaBan > 99_999_999.99m)
                return "Giá bán phải lớn hơn 0 và không vượt quá 99,999,999.99 VNĐ (decimal(10,2)).";

            return null;
        }

        private void NotifyChanged()
        {
            BooksChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
