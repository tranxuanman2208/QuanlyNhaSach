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

        public IReadOnlyList<string> StockFilterOptions { get; } = new List<string>
        {
            BookService.AllStockFilter,
            BookService.NormalStockFilter,
            BookService.LowStockFilter,
            BookService.OutOfStockFilter
        };

        public IReadOnlyList<string> CoverColorPresets { get; } = new List<string>
        {
            "#1E3A8A",
            "#0F766E",
            "#B45309",
            "#4338CA",
            "#7C2D12",
            "#1D4ED8",
            "#991B1B",
            "#065F46"
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
            RefreshBooks();
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
            FormMaSach = string.Empty;
            FormTenSach = string.Empty;
            FormTacGia = string.Empty;
            FormTheLoai = "Văn học & Kỹ năng";
            FormNXB = string.Empty;
            FormMaNCC = AvailableSuppliers.Count > 0 ? AvailableSuppliers[0].MaNCC : string.Empty;
            FormGiaBanText = "120000";
            FormSoLuongTonText = "15";
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
            FormErrorMessage = string.Empty;
            IsBookDialogOpen = true;
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
                FormErrorMessage = "Vui lòng nhập mã sách.";
                return;
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
                    soLuongTon);
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
                    soLuongTon);
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
