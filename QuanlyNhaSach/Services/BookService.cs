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

        public IReadOnlyList<SACH> GetAllBooks()
        {
            using (var db = new QuanLyNhaSachContext())
            {
                return db.SACHs.Where(s => s.Daxoa == false || s.Daxoa == null).ToList();
            }
        }

        public IReadOnlyList<SACH> GetLowStockBooks()
        {
            using (var db = new QuanLyNhaSachContext())
            {
                return db.SACHs
                         .Where(s => s.Soluongton < 10 && s.Soluongton > 0 && (s.Daxoa == false || s.Daxoa == null))
                         .OrderBy(s => s.Soluongton).ThenBy(s => s.MaSach)
                         .ToList();
            }
        }

        public int GetLowStockBookCount()
        {
            using (var db = new QuanLyNhaSachContext())
            {
                return db.SACHs.Count(s => s.Soluongton < 10 && s.Soluongton > 0 && (s.Daxoa == false || s.Daxoa == null));
            }
        }

        public SACH? GetBookById(string maSach)
        {
            using (var db = new QuanLyNhaSachContext())
            {
                // Tìm đúng mã sách
                return db.SACHs.FirstOrDefault(s => s.MaSach == maSach && (s.Daxoa == false || s.Daxoa == null));
            }
        }
        public IReadOnlyList<SACH> SearchAndFilterBooks(string? keyword, string? stockFilter = null)
        {
            using (var db = new QuanLyNhaSachContext())
            {
                // Chặn sách đã xóa
                var query = db.SACHs.Where(s => s.Daxoa == false || s.Daxoa == null);

                // Lọc theo từ khóa
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    query = query.Where(s => s.Tensach.Contains(keyword) || s.Tacgia.Contains(keyword) || s.MaSach.Contains(keyword));
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

        public OperationResult<SACH> AddBook(
            string maSachMoi, string tenSach, string tacGia, string theLoai,
            string nxb, string maNCC, decimal giaBan, byte soLuongTon)
        {
            using (var db = new QuanLyNhaSachContext())
            {
                bool isDuplicate = db.SACHs.Any(s => s.Tensach == tenSach && s.Tacgia == tacGia);
                if (isDuplicate) return OperationResult<SACH>.Failure("Sách này đã tồn tại trong hệ thống!");

                bool isIdExist = db.SACHs.Any(s => s.MaSach == maSachMoi);
                if (isIdExist) return OperationResult<SACH>.Failure("Mã sách này đã bị trùng!");

                var newBook = new SACH
                {
                    MaSach = maSachMoi,
                    Tensach = tenSach,
                    Tacgia = tacGia,
                    Theloai = theLoai,
                    NXB = nxb,
                    MaNCC = maNCC,
                    Giaban = giaBan,
                    Soluongton = soLuongTon
                };

                db.SACHs.Add(newBook);
                db.SaveChanges();
                NotifyChanged();
                return OperationResult<SACH>.Success(newBook, "Đã thêm sách mới thành công.");
            }
        }

        public OperationResult<SACH> UpdateBook(
            string maSach, string tenSach, string tacGia, string theLoai,
            string nxb, string maNCC, decimal giaBan, byte soLuongTon)
        {
            using (var db = new QuanLyNhaSachContext())
            {
                var existingBook = db.SACHs.FirstOrDefault(s => s.MaSach == maSach);
                if (existingBook == null) return OperationResult<SACH>.Failure("Không tìm thấy sách.");

                bool isDuplicate = db.SACHs.Any(b => b.MaSach != maSach && b.Tensach == tenSach && b.Tacgia == tacGia);
                if (isDuplicate) return OperationResult<SACH>.Failure("Tên sách và tác giả này đã tồn tại ở mã khác.");

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

        public OperationResult DeleteBook(string maSach)
        {
            using (var db = new QuanLyNhaSachContext())
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

        public List<string?> GetBookSuggestions(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return new List<string>();

            using (var db = new QuanLyNhaSachContext())
            {
                return db.SACHs
                         .Where(s => (s.Daxoa == false || s.Daxoa == null)
                                     && (s.Tensach.Contains(keyword) || s.MaSach.Contains(keyword) || s.Tacgia.Contains(keyword)))
                         .Select(s => s.Tensach) // Chỉ lấy cột Tên sách
                         .Distinct()             // Loại bỏ các tên bị trùng
                         .Take(10)               // Chỉ lấy tối đa 10 gợi ý đầu tiên
                         .ToList();
            }
        }

        private void NotifyChanged()
        {
            BooksChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}