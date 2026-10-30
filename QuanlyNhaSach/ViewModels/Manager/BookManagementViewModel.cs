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
        private readonly MockDataStore _dataStore;
        private readonly IDialogService _dialogService;

        public ObservableCollection<Book> DisplayedBooks { get; } = new();

        public IReadOnlyList<string> StockFilterOptions { get; } = new List<string>
        {
            BookService.AllStockFilter,
            BookService.NormalStockFilter,
            BookService.LowStockFilter,
            BookService.OutOfStockFilter,
            BookService.InactiveBookFilter
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
        private int _editingBookId;

        [ObservableProperty]
        private string _dialogTitle = "Thêm sách mới";

        [ObservableProperty]
        private string _formTenSach = string.Empty;

        [ObservableProperty]
        private string _formTacGia = string.Empty;

        [ObservableProperty]
        private string _formTheLoai = "Văn học & Kỹ năng";

        [ObservableProperty]
        private string _formGiaBanText = string.Empty;

        [ObservableProperty]
        private string _formSoLuongTonText = "10";

        [ObservableProperty]
        private string _formAnhBia = "BookCoverPlaceholder";

        [ObservableProperty]
        private string _formMauBia = "#1E3A8A";

        [ObservableProperty]
        private string _formMoTa = string.Empty;

        [ObservableProperty]
        private bool _formIsActive = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasFormError))]
        private string _formErrorMessage = string.Empty;

        public bool HasFormError => !string.IsNullOrWhiteSpace(FormErrorMessage);

        public BookManagementViewModel(
            BookService bookService,
            MockDataStore dataStore,
            IDialogService dialogService)
        {
            _bookService = bookService ?? throw new ArgumentNullException(nameof(bookService));
            _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            _dataStore.DataChanged += (_, _) => RefreshBooks();
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
            var books = _bookService.SearchAndFilterBooks(SearchKeyword, SelectedStockFilter, includeInactive: true);
            DisplayedBooks.Clear();
            foreach (var b in books)
            {
                DisplayedBooks.Add(b);
            }

            TotalDisplayedCount = DisplayedBooks.Count;
            LowStockCount = _bookService.GetLowStockBookCount();
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
            IsEditMode = false;
            EditingBookId = 0;
            DialogTitle = "Thêm đầu sách mới (UC09)";
            FormTenSach = string.Empty;
            FormTacGia = string.Empty;
            FormTheLoai = "Văn học & Kỹ năng";
            FormGiaBanText = "120000";
            FormSoLuongTonText = "15";
            FormAnhBia = "BookCoverPlaceholder";
            FormMauBia = "#1E3A8A";
            FormMoTa = string.Empty;
            FormIsActive = true;
            FormErrorMessage = string.Empty;
            IsBookDialogOpen = true;
        }

        [RelayCommand]
        public void OpenEditBookDialog(Book? book)
        {
            if (book == null)
            {
                return;
            }

            IsEditMode = true;
            EditingBookId = book.MaSach;
            DialogTitle = $"Chỉnh sửa thông tin sách #{book.MaSach}";
            FormTenSach = book.TenSach;
            FormTacGia = book.TacGia;
            FormTheLoai = book.TheLoai;
            FormGiaBanText = book.GiaBan.ToString("0", CultureInfo.InvariantCulture);
            FormSoLuongTonText = book.SoLuongTon.ToString(CultureInfo.InvariantCulture);
            FormAnhBia = book.AnhBia;
            FormMauBia = book.MauBia;
            FormMoTa = book.MoTa;
            FormIsActive = book.IsActive;
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

            string cleanPrice = FormGiaBanText
                .Replace("₫", string.Empty)
                .Replace(",", string.Empty)
                .Replace(".", string.Empty)
                .Trim();

            if (!decimal.TryParse(cleanPrice, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal giaBan) || giaBan <= 0)
            {
                FormErrorMessage = "Giá bán không hợp lệ. Vui lòng nhập số dương lớn hơn 0 (ví dụ: 120000).";
                return;
            }

            if (!int.TryParse(FormSoLuongTonText?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int soLuongTon) || soLuongTon < 0)
            {
                FormErrorMessage = "Số lượng tồn kho không hợp lệ. Vui lòng nhập số nguyên không âm (>= 0).";
                return;
            }

            OperationResult<Book> result;
            if (IsEditMode)
            {
                result = _bookService.UpdateBook(
                    EditingBookId,
                    FormTenSach,
                    FormTacGia,
                    FormTheLoai,
                    giaBan,
                    soLuongTon,
                    FormMauBia,
                    FormAnhBia,
                    FormMoTa,
                    FormIsActive);
            }
            else
            {
                result = _bookService.AddBook(
                    FormTenSach,
                    FormTacGia,
                    FormTheLoai,
                    giaBan,
                    soLuongTon,
                    FormMauBia,
                    FormAnhBia,
                    FormMoTa);
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
        public void DeleteBook(Book? book)
        {
            if (book == null)
            {
                return;
            }

            string actionVerb = book.IsActive ? "xóa hoặc ngừng kinh doanh" : "mở bán lại";
            bool confirmed = _dialogService.ShowConfirmation(
                "Xác nhận thao tác danh mục sách",
                $"Bạn có chắc chắn muốn {actionVerb} sách \"{book.TenSach}\" (Mã #{book.MaSach}) không?\n\n" +
                $"Lưu ý: Nếu sách đã phát sinh trong lịch sử Đơn hàng hoặc Phiếu nhập, hệ thống sẽ chuyển sang trạng thái 'Ngừng bán' để bảo toàn lịch sử.");

            if (!confirmed)
            {
                return;
            }

            var result = _bookService.DeleteOrDeactivateBook(book.MaSach);
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
