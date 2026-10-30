using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanlyNhaSach.Models;
using QuanlyNhaSach.Navigation;
using QuanlyNhaSach.Services;

namespace QuanlyNhaSach.ViewModels.Manager
{
    public partial class ImportHistoryViewModel : ObservableObject, INavigationAware
    {
        private readonly ImportService _importService;
        private readonly MockDataStore _dataStore;
        private readonly INavigationService _navigationService;

        public ObservableCollection<ImportReceipt> DisplayedReceipts { get; } = new();

        [ObservableProperty]
        private string _searchKeyword = string.Empty;

        [ObservableProperty]
        private int _totalReceiptsCount;

        [ObservableProperty]
        private string _totalHistoricalImportAmountText = "0 ₫";

        // ==================== RECEIPT DETAILS MODAL STATE ====================
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasSelectedReceipt))]
        private ImportReceipt? _selectedReceiptForDetail;

        [ObservableProperty]
        private bool _isDetailModalOpen;

        public bool HasSelectedReceipt => SelectedReceiptForDetail != null;

        public ImportHistoryViewModel(
            ImportService importService,
            MockDataStore dataStore,
            INavigationService navigationService)
        {
            _importService = importService ?? throw new ArgumentNullException(nameof(importService));
            _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            _dataStore.DataChanged += (_, _) => RefreshHistory();
            _importService.ImportReceiptConfirmed += (_, _) => RefreshHistory();

            RefreshHistory();
        }

        partial void OnSearchKeywordChanged(string value)
        {
            RefreshHistory();
        }

        public void OnNavigatedTo(object? parameter)
        {
            IsDetailModalOpen = false;
            RefreshHistory();
        }

        [RelayCommand]
        public void RefreshHistory()
        {
            var receipts = _importService.SearchConfirmedReceipts(SearchKeyword);
            DisplayedReceipts.Clear();
            foreach (var r in receipts)
            {
                DisplayedReceipts.Add(r);
            }

            TotalReceiptsCount = DisplayedReceipts.Count;
            decimal sum = DisplayedReceipts.Sum(r => r.TongTien);
            TotalHistoricalImportAmountText = $"{sum:N0} ₫";
        }

        [RelayCommand]
        private void ClearSearch()
        {
            SearchKeyword = string.Empty;
            RefreshHistory();
        }

        [RelayCommand]
        public void ViewReceiptDetails(ImportReceipt? receipt)
        {
            if (receipt == null)
            {
                return;
            }

            SelectedReceiptForDetail = receipt;
            IsDetailModalOpen = true;
        }

        [RelayCommand]
        public void CloseReceiptDetails()
        {
            IsDetailModalOpen = false;
        }

        [RelayCommand]
        private void GoToCreateImportReceipt()
        {
            _navigationService.NavigateToImportReceipt();
        }
    }
}
