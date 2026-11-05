using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanlyNhaSach.Models;
using QuanlyNhaSach.Navigation;
using QuanlyNhaSach.Services;

namespace QuanlyNhaSach.ViewModels.Manager
{
    public partial class ImportReceiptViewModel : ObservableObject, INavigationAware
    {
        private readonly ImportService _importService;
        private readonly SupplierService _supplierService;
        private readonly BookService _bookService;
        private readonly AuthService _authService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;

        private bool _isConfirming;

        public ObservableCollection<NHACUNGCAP> ActiveSuppliers { get; } = new();
        public ObservableCollection<SACH> MatchedBooks { get; } = new();
        public ObservableCollection<CHITIETPHIEUNHAP> DraftDetails { get; } = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedSupplierDisplay))]
        private NHACUNGCAP? _selectedSupplier;

        [ObservableProperty]
        private string _bookSearchKeyword = string.Empty;

        [ObservableProperty]
        private SACH? _selectedSearchBook;

        [ObservableProperty]
        private string _draftGhiChu = "Nhập bổ sung sách vào kho";

        [ObservableProperty]
        private string _draftReceiptCode = "PN0001 (Bản nháp)";

        [ObservableProperty]
        private string _draftDateDisplay = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasValidationError))]
        private string _validationErrorMessage = string.Empty;

        public bool HasValidationError => !string.IsNullOrWhiteSpace(ValidationErrorMessage);

        public string SelectedSupplierDisplay => SelectedSupplier != null
            ? $"{SelectedSupplier.TenNCC} ({SelectedSupplier.SupplierCode})"
            : "Chưa chọn nhà cung cấp";

        public int DistinctBooksCount => DraftDetails.Count;
        public int TotalDraftQuantity => DraftDetails.Sum(d => Math.Max(0, d.SoLuongNhap));
        public decimal GrandTotal => DraftDetails.Sum(d => d.ThanhTien);
        public string FormattedGrandTotal => $"{GrandTotal:N0} ₫";
        public bool HasDraftLines => DraftDetails.Count > 0;
        public bool IsDraftEmpty => DraftDetails.Count == 0;

        public ImportReceiptViewModel(
            ImportService importService,
            SupplierService supplierService,
            BookService bookService,
            AuthService authService,
            INavigationService navigationService,
            IDialogService dialogService)
        {
            _importService = importService ?? throw new ArgumentNullException(nameof(importService));
            _supplierService = supplierService ?? throw new ArgumentNullException(nameof(supplierService));
            _bookService = bookService ?? throw new ArgumentNullException(nameof(bookService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            DraftDetails.CollectionChanged += OnDraftDetailsCollectionChanged;
            _bookService.BooksChanged += (_, _) =>
            {
                RefreshBookSearch();
                RefreshDraftCurrentStocks();
            };

            InitializeHeaderState();
            RefreshSuppliers();
            RefreshBookSearch();
        }

        partial void OnBookSearchKeywordChanged(string value)
        {
            RefreshBookSearch();
        }

        public void OnNavigatedTo(object? parameter)
        {
            ValidationErrorMessage = string.Empty;
            InitializeHeaderState();
            RefreshSuppliers();
            RefreshBookSearch();
            RefreshDraftCurrentStocks();
        }

        private void InitializeHeaderState()
        {
            int nextId = _importService.PeekNextReceiptId();
            DraftReceiptCode = $"PN{nextId:D4} (Bản nháp)";
            DraftDateDisplay = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        }

        public void RefreshSuppliers()
        {
            string? prevSupplierId = SelectedSupplier?.MaNCC;
            var active = _supplierService.GetActiveSuppliers();

            ActiveSuppliers.Clear();
            foreach (var s in active)
            {
                ActiveSuppliers.Add(s);
            }

            if (!string.IsNullOrWhiteSpace(prevSupplierId))
            {
                SelectedSupplier = ActiveSuppliers.FirstOrDefault(s =>
                                       string.Equals(s.MaNCC, prevSupplierId, StringComparison.OrdinalIgnoreCase))
                                   ?? ActiveSuppliers.FirstOrDefault();
            }
            else if (SelectedSupplier == null && ActiveSuppliers.Count > 0)
            {
                SelectedSupplier = ActiveSuppliers[0];
            }
        }

        public void RefreshBookSearch()
        {
            var books = _bookService.SearchAndFilterBooks(BookSearchKeyword);
            MatchedBooks.Clear();
            foreach (var b in books)
            {
                MatchedBooks.Add(b);
            }
        }

        private void RefreshDraftCurrentStocks()
        {
            foreach (var line in DraftDetails)
            {
                var sharedBook = _bookService.GetBookById(line.MaSach);
                if (sharedBook != null)
                {
                    line.CurrentStock = sharedBook.Soluongton ?? 0;
                }
            }
        }

        private void OnDraftDetailsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (CHITIETPHIEUNHAP oldItem in e.OldItems)
                {
                    oldItem.LineValueChanged -= OnDraftLineValueChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (CHITIETPHIEUNHAP newItem in e.NewItems)
                {
                    newItem.LineValueChanged += OnDraftLineValueChanged;
                }
            }

            NotifySummaryProperties();
        }

        private void OnDraftLineValueChanged(object? sender, EventArgs e)
        {
            ValidationErrorMessage = string.Empty;
            NotifySummaryProperties();
        }

        private void NotifySummaryProperties()
        {
            OnPropertyChanged(nameof(DistinctBooksCount));
            OnPropertyChanged(nameof(TotalDraftQuantity));
            OnPropertyChanged(nameof(GrandTotal));
            OnPropertyChanged(nameof(FormattedGrandTotal));
            OnPropertyChanged(nameof(HasDraftLines));
            OnPropertyChanged(nameof(IsDraftEmpty));
        }

        [RelayCommand]
        public void AddBookToDraft(SACH? book)
        {
            var targetBook = book ?? SelectedSearchBook;
            if (targetBook == null)
            {
                return;
            }

            ValidationErrorMessage = string.Empty;

            // Prevent duplicate lines: if book already exists in draft, increment quantity by 5
            var existingLine = DraftDetails.FirstOrDefault(d =>
                string.Equals(d.MaSach, targetBook.MaSach, StringComparison.OrdinalIgnoreCase));
            if (existingLine != null)
            {
                existingLine.SoLuongNhap += 5;
                NotifySummaryProperties();
                return;
            }

            // Default import unit price to ~68% of selling price (rounded to nearest 1,000 VND)
            decimal sellingPrice = targetBook.Giaban ?? 0m;
            decimal suggestedImportPrice = Math.Max(10000m, Math.Round((sellingPrice * 0.68m) / 1000m, 0) * 1000m);

            var newLine = new CHITIETPHIEUNHAP
            {
                MaSach = targetBook.MaSach,
                TenSach = targetBook.Tensach ?? string.Empty,
                TacGia = targetBook.Tacgia ?? string.Empty,
                CurrentStock = targetBook.Soluongton ?? 0,
                SoLuongNhap = 10,
                DonGiaNhap = suggestedImportPrice
            };

            DraftDetails.Add(newLine);
            NotifySummaryProperties();
        }

        [RelayCommand]
        public void RemoveDraftLine(CHITIETPHIEUNHAP? detailLine)
        {
            if (detailLine == null)
            {
                return;
            }

            DraftDetails.Remove(detailLine);
            NotifySummaryProperties();
        }

        [RelayCommand]
        public void ClearDraft()
        {
            if (DraftDetails.Count == 0)
            {
                return;
            }

            if (_dialogService.ShowConfirmation(
                "Xác nhận làm mới phiếu nháp",
                "Bạn có chắc chắn muốn xóa toàn bộ danh sách sách trong phiếu nhập nháp hiện tại không?"))
            {
                DraftDetails.Clear();
                ValidationErrorMessage = string.Empty;
                InitializeHeaderState();
                NotifySummaryProperties();
            }
        }

        [RelayCommand]
        public void ConfirmImport()
        {
            if (_isConfirming)
            {
                return;
            }

            ValidationErrorMessage = string.Empty;

            var validation = _importService.ValidateDraft(SelectedSupplier, DraftDetails);
            if (!validation.IsSuccess)
            {
                ValidationErrorMessage = validation.Message;
                _dialogService.ShowError("Dữ liệu phiếu nhập chưa hợp lệ", validation.Message);
                return;
            }

            string confirmPrompt =
                $"Bạn có chắc chắn muốn XÁC NHẬN NHẬP HÀNG cho phiếu nhập này không?\n\n" +
                $"• Nhà cung cấp: {SelectedSupplier!.TenNCC}\n" +
                $"• Số đầu sách: {DistinctBooksCount} đầu sách\n" +
                $"• Tổng số lượng nhập: {TotalDraftQuantity} cuốn\n" +
                $"• Tổng tiền thanh toán: {FormattedGrandTotal}\n\n" +
                $"Sau khi xác nhận, số lượng tồn kho của các đầu sách sẽ được cộng thêm tương ứng.";

            if (!_dialogService.ShowConfirmation("Xác nhận lập phiếu nhập hàng", confirmPrompt))
            {
                return;
            }

            _isConfirming = true;
            try
            {
                var result = _importService.ConfirmImportReceipt(
                    SelectedSupplier,
                    DraftDetails.ToList(),
                    _authService.CurrentUser,
                    DraftGhiChu);

                if (!result.IsSuccess || result.Data == null)
                {
                    ValidationErrorMessage = result.Message;
                    _dialogService.ShowError("Xác nhận nhập hàng thất bại", result.Message);
                    return;
                }

                DraftDetails.Clear();
                DraftGhiChu = "Nhập bổ sung sách vào kho";
                InitializeHeaderState();
                RefreshBookSearch();
                NotifySummaryProperties();

                _dialogService.ShowInformation("Nhập hàng thành công", result.Message);
            }
            finally
            {
                _isConfirming = false;
            }
        }

        [RelayCommand]
        private void GoToImportHistory()
        {
            _navigationService.NavigateToImportHistory();
        }
    }
}
