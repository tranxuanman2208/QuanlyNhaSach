using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanlyNhaSach.Models;
using QuanlyNhaSach.Navigation;
using QuanlyNhaSach.Services;

namespace QuanlyNhaSach.ViewModels.Manager
{
    public partial class BookManagementViewModel : ObservableObject, INavigationAware
    {
        private readonly BookService _bookService;
        private readonly IDialogService _dialogService;

        public ObservableCollection<SACH> DisplayedBooks { get; } = new();
        public ObservableCollection<NHACUNGCAP> AvailableSuppliers { get; } = new();
        public ObservableCollection<string> BookTitleSuggestions { get; } = new();

        [ObservableProperty]
        private bool _isBookSuggestionsOpen;

        public IReadOnlyList<string> StockFilterOptions { get; } = new List<string>
        {
            BookService.AllStockFilter,
            BookService.NormalStockFilter,
            BookService.LowStockFilter,
            BookService.OutOfStockFilter
        };

        [ObservableProperty]
        private string _searchKeyword = string.Empty;

        [ObservableProperty]
        private string _selectedStockFilter = BookService.AllStockFilter;

        [ObservableProperty]
        private int _totalDisplayedCount;

        [ObservableProperty]
        private int _lowStockCount;

        // ==================== ADD / EDIT DIALOG STATE ====================
        [ObservableProperty]
        private bool _isBookDialogOpen;

        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private string _editingBookId = string.Empty;

        [ObservableProperty]
        private string _dialogTitle = "Thêm sách mới";

        [ObservableProperty]
        private string _formMaSach = string.Empty;

        [ObservableProperty]
        private string _formTenSach = string.Empty;

        [ObservableProperty]
        private string _formTacGia = string.Empty;

        [ObservableProperty]
        private string _formTheLoai = "Văn học & Kỹ năng";

        [ObservableProperty]
        private string _formNXB = string.Empty;

        [ObservableProperty]
        private string _formMaNCC = string.Empty;

        [ObservableProperty]
        private string _formGiaBanText = string.Empty;

        [ObservableProperty]
        private string _formSoLuongTonText = "10";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasFormImagePreview))]
        private string _formHinhAnh = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasFormImagePreview))]
        private string _formHinhAnhPreview = string.Empty;

        public bool HasFormImagePreview => !string.IsNullOrWhiteSpace(FormHinhAnhPreview);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasFormError))]
        private string _formErrorMessage = string.Empty;

        [ObservableProperty]
        private string _supplierHintText = string.Empty;

        public bool HasFormError => !string.IsNullOrWhiteSpace(FormErrorMessage);

        public BookManagementViewModel(
            BookService bookService,
            IDialogService dialogService)
        {
            _bookService = bookService ?? throw new ArgumentNullException(nameof(bookService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            _bookService.BooksChanged += (_, _) => RefreshBooks();

            RefreshBooks();
        }

        partial void OnSearchKeywordChanged(string value)
        {
            UpdateBookSuggestions(value);
            RefreshBooks();
        }

        private void UpdateBookSuggestions(string keyword)
        {
            BookTitleSuggestions.Clear();
            if (string.IsNullOrWhiteSpace(keyword) || keyword.Trim().Length < 1)
            {
                IsBookSuggestionsOpen = false;
                return;
            }

            string term = keyword.Trim().ToLowerInvariant();
            var allBooks = _bookService.GetAllBooks();
            var matches = allBooks
                .Where(b => (!string.IsNullOrWhiteSpace(b.Tensach) && b.Tensach.ToLowerInvariant().Contains(term)) ||
                            (!string.IsNullOrWhiteSpace(b.MaSach) && b.MaSach.ToLowerInvariant().Contains(term)))
                .Select(b => b.Tensach!.Trim())
                .Distinct()
                .Take(6)
                .ToList();

            foreach (var title in matches)
            {
                BookTitleSuggestions.Add(title);
            }

            IsBookSuggestionsOpen = BookTitleSuggestions.Count > 0;
        }

        [RelayCommand]
        public void SelectSuggestion(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return;
            SearchKeyword = title;
            IsBookSuggestionsOpen = false;
        }

        [RelayCommand]
        public void CloseSuggestions()
        {
            IsBookSuggestionsOpen = false;
        }

        partial void OnSelectedStockFilterChanged(string value)
        {
            RefreshBooks();
        }

        public void OnNavigatedTo(object? parameter)
        {
            IsBookDialogOpen = false;
            RefreshBooks();
        }

        [RelayCommand]
        public void RefreshBooks()
        {
            var books = _bookService.SearchAndFilterBooks(SearchKeyword, SelectedStockFilter);
            DisplayedBooks.Clear();
            foreach (var b in books)
            {
                DisplayedBooks.Add(b);
            }

            TotalDisplayedCount = DisplayedBooks.Count;
            LowStockCount = _bookService.GetLowStockBookCount();
            RefreshSupplierChoices();
        }

        private void RefreshSupplierChoices()
        {
            var suppliers = _bookService.GetAvailableSuppliers();
            AvailableSuppliers.Clear();
            foreach (var s in suppliers)
            {
                AvailableSuppliers.Add(s);
            }

            if (AvailableSuppliers.Count > 0)
            {
                var codes = new List<string>();
                foreach (var s in AvailableSuppliers)
                {
                    if (!string.IsNullOrWhiteSpace(s.MaNCC))
                    {
                        codes.Add(string.IsNullOrWhiteSpace(s.TenNCC) ? s.MaNCC : $"{s.MaNCC} ({s.TenNCC})");
                    }
                }
                SupplierHintText = $"Mã NCC hợp lệ trong DB: {string.Join(", ", codes)}";
            }
            else
            {
                SupplierHintText = "Lưu ý: Cần nhập MaNCC đã tồn tại trong bảng NHACUNGCAP.";
            }
        }

        [RelayCommand]
        private void ClearFilters()
        {
            SearchKeyword = string.Empty;
            SelectedStockFilter = BookService.AllStockFilter;
            RefreshBooks();
        }

        [RelayCommand]
        public void OpenAddBookDialog()
        {
            RefreshSupplierChoices();
            IsEditMode = false;
            EditingBookId = string.Empty;
            DialogTitle = "Thêm đầu sách mới (UC09)";
            FormMaSach = _bookService.GenerateNextBookId();
            FormTenSach = string.Empty;
            FormTacGia = string.Empty;
            FormTheLoai = string.Empty;
            FormNXB = string.Empty;
            FormMaNCC = AvailableSuppliers.Count > 0 ? AvailableSuppliers[0].MaNCC : string.Empty;
            FormGiaBanText = string.Empty;
            FormSoLuongTonText = string.Empty;
            FormHinhAnh = string.Empty;
            FormHinhAnhPreview = string.Empty;
            FormErrorMessage = string.Empty;
            IsBookDialogOpen = true;
        }

        [RelayCommand]
        public void OpenEditBookDialog(SACH? book)
        {
            if (book == null)
            {
                return;
            }

            RefreshSupplierChoices();
            IsEditMode = true;
            EditingBookId = book.MaSach;
            DialogTitle = $"Chỉnh sửa thông tin sách #{book.MaSach}";
            FormMaSach = book.MaSach;
            FormTenSach = book.Tensach ?? string.Empty;
            FormTacGia = book.Tacgia ?? string.Empty;
            FormTheLoai = book.Theloai ?? string.Empty;
            FormNXB = book.NXB ?? string.Empty;
            FormMaNCC = book.MaNCC ?? string.Empty;
            FormGiaBanText = (book.Giaban ?? 0m).ToString("0", CultureInfo.InvariantCulture);
            FormSoLuongTonText = (book.Soluongton ?? 0).ToString(CultureInfo.InvariantCulture);
            FormHinhAnh = book.Hinhanh ?? string.Empty;
            FormHinhAnhPreview = book.DisplayHinhAnh ?? string.Empty;
            FormErrorMessage = string.Empty;
            IsBookDialogOpen = true;
        }

        [RelayCommand]
        public void PickImage()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Chọn ảnh bìa sách",
                Filter = "Tệp hình ảnh (*.png;*.jpg;*.jpeg;*.webp)|*.png;*.jpg;*.jpeg;*.webp|Tất cả tệp (*.*)|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string sourceFilePath = dialog.FileName;
                    string extension = System.IO.Path.GetExtension(sourceFilePath);
                    string cleanFileName = System.IO.Path.GetFileNameWithoutExtension(sourceFilePath);
                    string codePrefix = (FormMaSach ?? string.Empty).Trim();
                    if (!string.IsNullOrWhiteSpace(codePrefix) && cleanFileName.StartsWith(codePrefix + "_", StringComparison.OrdinalIgnoreCase))
                    {
                        cleanFileName = cleanFileName.Substring(codePrefix.Length + 1);
                    }
                    string targetFileName = string.IsNullOrWhiteSpace(codePrefix)
                        ? $"book_{DateTime.Now:yyyyMMdd_HHmmss}{extension}"
                        : $"{codePrefix}_{cleanFileName}{extension}";

                    targetFileName = targetFileName.Replace(" ", "_");

                    // 1. Sao chép vào thư mục Img của project nguồn (để lưu trữ lâu dài)
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    string projectDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", ".."));
                    string projectImgDir = System.IO.Path.Combine(projectDir, "Img");
                    if (System.IO.Directory.Exists(projectImgDir))
                    {
                        string targetProjectPath = System.IO.Path.Combine(projectImgDir, targetFileName);
                        System.IO.File.Copy(sourceFilePath, targetProjectPath, overwrite: true);
                    }

                    // 2. Sao chép vào thư mục Img tại output build (bin) để WPF hiển thị ngay lập tức
                    string binImgDir = System.IO.Path.Combine(baseDir, "Img");
                    System.IO.Directory.CreateDirectory(binImgDir);
                    string targetBinPath = System.IO.Path.Combine(binImgDir, targetFileName);
                    System.IO.File.Copy(sourceFilePath, targetBinPath, overwrite: true);

                    FormHinhAnh = targetFileName;
                    FormHinhAnhPreview = targetBinPath;
                }
                catch (Exception ex)
                {
                    FormErrorMessage = $"Không thể tải ảnh lên: {ex.Message}";
                }
            }
        }

        [RelayCommand]
        public void ClearImage()
        {
            FormHinhAnh = string.Empty;
            FormHinhAnhPreview = string.Empty;
        }

        [RelayCommand]
        public void CloseBookDialog()
        {
            IsBookDialogOpen = false;
            FormErrorMessage = string.Empty;
        }

        [RelayCommand]
        public void SaveBook()
        {
            FormErrorMessage = string.Empty;

            string maSach = (IsEditMode ? EditingBookId : FormMaSach)?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(maSach))
            {
                maSach = _bookService.GenerateNextBookId();
                FormMaSach = maSach;
            }

            if (maSach.Length > 10)
            {
                FormErrorMessage = "Mã sách không được vượt quá 10 ký tự.";
                return;
            }

            if (string.IsNullOrWhiteSpace(FormTenSach))
            {
                FormErrorMessage = "Vui lòng nhập tên đầu sách.";
                return;
            }

            if (string.IsNullOrWhiteSpace(FormTacGia))
            {
                FormErrorMessage = "Vui lòng nhập tên tác giả.";
                return;
            }

            if (string.IsNullOrWhiteSpace(FormNXB))
            {
                FormErrorMessage = "Vui lòng nhập nhà xuất bản.";
                return;
            }

            if (string.IsNullOrWhiteSpace(FormMaNCC))
            {
                FormErrorMessage = "Vui lòng nhập mã nhà cung cấp.";
                return;
            }

            if (FormMaNCC.Trim().Length > 10)
            {
                FormErrorMessage = "Mã nhà cung cấp không được vượt quá 10 ký tự.";
                return;
            }

            string cleanPrice = FormGiaBanText
                .Replace("₫", string.Empty)
                .Replace(",", string.Empty)
                .Replace(".", string.Empty)
                .Trim();

            if (!decimal.TryParse(cleanPrice, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal giaBan) || giaBan <= 0 || giaBan > 99_999_999.99m)
            {
                FormErrorMessage = "Giá bán không hợp lệ. Vui lòng nhập số dương từ 1 đến 99,999,999 (ví dụ: 120000).";
                return;
            }

            if (!byte.TryParse(FormSoLuongTonText?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out byte soLuongTon))
            {
                FormErrorMessage = "Số lượng tồn kho không hợp lệ. Vui lòng nhập số nguyên từ 0 đến 255.";
                return;
            }

            string? hinhAnhValue = string.IsNullOrWhiteSpace(FormHinhAnh) ? null : FormHinhAnh.Trim();

            OperationResult<SACH> result;
            if (IsEditMode)
            {
                result = _bookService.UpdateBook(
                    EditingBookId,
                    FormTenSach.Trim(),
                    FormTacGia.Trim(),
                    FormTheLoai.Trim(),
                    FormNXB.Trim(),
                    FormMaNCC.Trim(),
                    giaBan,
                    soLuongTon,
                    hinhAnhValue);
            }
            else
            {
                result = _bookService.AddBook(
                    maSach,
                    FormTenSach.Trim(),
                    FormTacGia.Trim(),
                    FormTheLoai.Trim(),
                    FormNXB.Trim(),
                    FormMaNCC.Trim(),
                    giaBan,
                    soLuongTon,
                    hinhAnhValue);
            }

            if (!result.IsSuccess)
            {
                FormErrorMessage = result.Message;
                return;
            }

            IsBookDialogOpen = false;
            RefreshBooks();
            _dialogService.ShowInformation("Quản lý sách (UC09)", result.Message);
        }

        [RelayCommand]
        public void DeleteBook(SACH? book)
        {
            if (book == null)
            {
                return;
            }

            bool confirmed = _dialogService.ShowConfirmation(
                "Xác nhận thao tác danh mục sách",
                $"Bạn có chắc chắn muốn xóa hoặc ngừng kinh doanh sách \"{book.Tensach}\" (Mã #{book.MaSach}) không?\n\n" +
                $"Lưu ý: Nếu sách đã phát sinh trong lịch sử Đơn hàng, Giỏ hàng hoặc Phiếu nhập, hệ thống sẽ chuyển sang xóa mềm để bảo toàn lịch sử.");

            if (!confirmed)
            {
                return;
            }

            var result = _bookService.DeleteBook(book.MaSach);
            if (!result.IsSuccess)
            {
                _dialogService.ShowError("Không thể thực hiện", result.Message);
                return;
            }

            RefreshBooks();
            _dialogService.ShowInformation("Cập nhật danh mục sách", result.Message);
        }
    }
}
