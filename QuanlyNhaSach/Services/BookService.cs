using System;
using System.Collections.Generic;
using System.Linq;
using QuanlyNhaSach.Models;
using QuanlyNhaSach.Models.Enums;

namespace QuanlyNhaSach.Services
{
    public class BookService
    {
        public const string AllStockFilter = "Tất cả";
        public const string NormalStockFilter = "Đủ hàng (>= 10)";
        public const string LowStockFilter = "Sắp hết (< 10)";
        public const string OutOfStockFilter = "Hết hàng (0)";
        public const string InactiveBookFilter = "Ngừng bán";

        private readonly MockDataStore _dataStore;

        public event EventHandler? BooksChanged;

        public BookService(MockDataStore dataStore)
        {
            _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
        }

        public IReadOnlyList<Book> GetAllBooks(bool includeInactive = true)
        {
            return _dataStore.GetAllBooks(includeInactive);
        }

        public Book? GetBookById(int maSach)
        {
            return _dataStore.GetBookById(maSach);
        }

        public IReadOnlyList<Book> GetLowStockBooks()
        {
            return _dataStore.Books
                .Where(b => b.IsActive && b.SoLuongTon < 10)
                .OrderBy(b => b.SoLuongTon)
                .ThenBy(b => b.MaSach)
                .ToList();
        }

        public int GetLowStockBookCount()
        {
            return _dataStore.Books.Count(b => b.IsActive && b.SoLuongTon < 10);
        }

        public IReadOnlyList<Book> SearchAndFilterBooks(string? keyword, string? stockFilter = null, bool includeInactive = true)
        {
            IEnumerable<Book> query = _dataStore.SearchBooks(keyword ?? string.Empty, includeInactive);

            if (!string.IsNullOrWhiteSpace(stockFilter) &&
                !stockFilter.Equals(AllStockFilter, StringComparison.OrdinalIgnoreCase))
            {
                if (stockFilter.Equals(NormalStockFilter, StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(b => b.IsActive && b.SoLuongTon >= 10);
                }
                else if (stockFilter.Equals(LowStockFilter, StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(b => b.IsActive && b.SoLuongTon < 10);
                }
                else if (stockFilter.Equals(OutOfStockFilter, StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(b => b.IsActive && b.SoLuongTon <= 0);
                }
                else if (stockFilter.Equals(InactiveBookFilter, StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(b => !b.IsActive);
                }
            }

            return query.OrderBy(b => b.MaSach).ToList();
        }

        public OperationResult<Book> AddBook(
            string? tenSach,
            string? tacGia,
            string? theLoai,
            decimal giaBan,
            int soLuongTon,
            string? mauBia = null,
            string? anhBia = null,
            string? moTa = null)
        {
            if (string.IsNullOrWhiteSpace(tenSach))
            {
                return OperationResult<Book>.Failure("Vui lòng nhập tên sách.");
            }

            if (string.IsNullOrWhiteSpace(tacGia))
            {
                return OperationResult<Book>.Failure("Vui lòng nhập tên tác giả.");
            }

            if (giaBan <= 0)
            {
                return OperationResult<Book>.Failure("Giá bán phải lớn hơn 0 ₫.");
            }

            if (soLuongTon < 0)
            {
                return OperationResult<Book>.Failure("Số lượng tồn kho ban đầu không được âm.");
            }

            string normalizedTitle = tenSach.Trim();
            string normalizedAuthor = tacGia.Trim();

            bool duplicateExists = _dataStore.Books.Any(b =>
                b.TenSach.Equals(normalizedTitle, StringComparison.OrdinalIgnoreCase) &&
                b.TacGia.Equals(normalizedAuthor, StringComparison.OrdinalIgnoreCase));

            if (duplicateExists)
            {
                return OperationResult<Book>.Failure(
                    $"Sách \"{normalizedTitle}\" của tác giả \"{normalizedAuthor}\" đã tồn tại trong danh mục.");
            }

            var newBook = new Book
            {
                MaSach = _dataStore.GenerateNextBookId(),
                TenSach = normalizedTitle,
                TacGia = normalizedAuthor,
                TheLoai = string.IsNullOrWhiteSpace(theLoai) ? "Sách tổng hợp" : theLoai.Trim(),
                GiaBan = giaBan,
                SoLuongTon = soLuongTon,
                MauBia = string.IsNullOrWhiteSpace(mauBia) ? "#1E3A8A" : mauBia.Trim(),
                AnhBia = string.IsNullOrWhiteSpace(anhBia) ? "DefaultBookCover" : anhBia.Trim(),
                MoTa = string.IsNullOrWhiteSpace(moTa)
                    ? $"Tác phẩm \"{normalizedTitle}\" của tác giả {normalizedAuthor}."
                    : moTa.Trim(),
                IsActive = true
            };

            _dataStore.Books.Add(newBook);
            NotifyChanged();

            return OperationResult<Book>.Success(
                newBook,
                $"Đã thêm sách mới \"{newBook.TenSach}\" (Mã #{newBook.MaSach}) vào hệ thống.");
        }

        public OperationResult<Book> UpdateBook(
            int maSach,
            string? tenSach,
            string? tacGia,
            string? theLoai,
            decimal giaBan,
            int soLuongTon,
            string? mauBia = null,
            string? anhBia = null,
            string? moTa = null,
            bool isActive = true)
        {
            var existingBook = _dataStore.GetBookById(maSach);
            if (existingBook == null)
            {
                return OperationResult<Book>.Failure($"Không tìm thấy sách có mã #{maSach} trong hệ thống.");
            }

            if (string.IsNullOrWhiteSpace(tenSach))
            {
                return OperationResult<Book>.Failure("Tên sách không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(tacGia))
            {
                return OperationResult<Book>.Failure("Tên tác giả không được để trống.");
            }

            if (giaBan <= 0)
            {
                return OperationResult<Book>.Failure("Giá bán phải lớn hơn 0 ₫.");
            }

            if (soLuongTon < 0)
            {
                return OperationResult<Book>.Failure("Số lượng tồn kho không được nhỏ hơn 0.");
            }

            string normalizedTitle = tenSach.Trim();
            string normalizedAuthor = tacGia.Trim();

            bool duplicateExists = _dataStore.Books.Any(b =>
                b.MaSach != maSach &&
                b.TenSach.Equals(normalizedTitle, StringComparison.OrdinalIgnoreCase) &&
                b.TacGia.Equals(normalizedAuthor, StringComparison.OrdinalIgnoreCase));

            if (duplicateExists)
            {
                return OperationResult<Book>.Failure(
                    $"Đã có một đầu sách khác mang tên \"{normalizedTitle}\" của tác giả \"{normalizedAuthor}\".");
            }

            // Update shared instance in-place so Customer and Staff portals observe changes immediately
            // while historical OrderItem and ImportReceiptDetail snapshots remain untouched.
            existingBook.TenSach = normalizedTitle;
            existingBook.TacGia = normalizedAuthor;
            if (!string.IsNullOrWhiteSpace(theLoai))
            {
                existingBook.TheLoai = theLoai.Trim();
            }
            existingBook.GiaBan = giaBan;
            existingBook.SoLuongTon = soLuongTon;
            if (!string.IsNullOrWhiteSpace(mauBia))
            {
                existingBook.MauBia = mauBia.Trim();
            }
            if (!string.IsNullOrWhiteSpace(anhBia))
            {
                existingBook.AnhBia = anhBia.Trim();
            }
            if (moTa != null)
            {
                existingBook.MoTa = moTa.Trim();
            }
            existingBook.IsActive = isActive;

            NotifyChanged();

            return OperationResult<Book>.Success(
                existingBook,
                $"Đã cập nhật thông tin sách \"{existingBook.TenSach}\" (Mã #{existingBook.MaSach}).");
        }

        public OperationResult DeleteOrDeactivateBook(int maSach)
        {
            var book = _dataStore.GetBookById(maSach);
            if (book == null)
            {
                return OperationResult.Failure($"Không tìm thấy sách có mã #{maSach}.");
            }

            bool referencedInOrders = _dataStore.Orders.Any(
                o => o.Items != null && o.Items.Any(i => i.MaSach == maSach));

            bool referencedInImports = _dataStore.ImportReceipts.Any(
                r => r.Details != null && r.Details.Any(d => d.MaSach == maSach));

            if (referencedInOrders || referencedInImports)
            {
                if (!book.IsActive)
                {
                    book.IsActive = true;
                    NotifyChanged();
                    return OperationResult.Success(
                        $"Đã mở bán lại sách \"{book.TenSach}\" (Mã #{book.MaSach}) trên danh mục.");
                }

                book.IsActive = false;
                NotifyChanged();
                return OperationResult.Success(
                    $"Sách \"{book.TenSach}\" (Mã #{book.MaSach}) đã phát sinh dữ liệu trong lịch sử Đơn hàng / Phiếu nhập.\n\n" +
                    $"Để bảo toàn tính toàn vẹn dữ liệu lịch sử, hệ thống đã chuyển trạng thái sách sang \"Ngừng bán\" thay vì xóa vật lý.");
            }

            _dataStore.Books.Remove(book);
            NotifyChanged();
            return OperationResult.Success($"Đã xóa sách \"{book.TenSach}\" (Mã #{book.MaSach}) khỏi danh mục.");
        }

        private void NotifyChanged()
        {
            BooksChanged?.Invoke(this, EventArgs.Empty);
            _dataStore.NotifyDataChanged();
        }
    }
}
