using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanlyNhaSach.Models;
using QuanlyNhaSach.Models.Enums;
using QuanlyNhaSach.Navigation;
using QuanlyNhaSach.Services;

namespace QuanlyNhaSach.ViewModels.Manager
{
    public partial class CustomerManagementViewModel : ObservableObject, INavigationAware
    {
        private readonly UserManagementService _userManagementService;
        private readonly AuthService _authService;
        private readonly MockDataStore _dataStore;
        private readonly IDialogService _dialogService;

        public ObservableCollection<User> DisplayedCustomers { get; } = new();

        public IReadOnlyList<string> StatusFilterOptions { get; } = new List<string>
        {
            UserManagementService.AllStatusFilter,
            UserManagementService.ActiveStatusFilter,
            UserManagementService.LockedStatusFilter
        };

        [ObservableProperty]
        private string _searchKeyword = string.Empty;

        [ObservableProperty]
        private string _selectedStatusFilter = UserManagementService.AllStatusFilter;

        [ObservableProperty]
        private int _totalDisplayedCount;

        [ObservableProperty]
        private int _activeCustomerCount;

        [ObservableProperty]
        private int _lockedCustomerCount;

        public CustomerManagementViewModel(
            UserManagementService userManagementService,
            AuthService authService,
            MockDataStore dataStore,
            IDialogService dialogService)
        {
            _userManagementService = userManagementService ?? throw new ArgumentNullException(nameof(userManagementService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            _dataStore.DataChanged += (_, _) => RefreshCustomers();
            _userManagementService.UsersChanged += (_, _) => RefreshCustomers();

            RefreshCustomers();
        }

        partial void OnSearchKeywordChanged(string value)
        {
            RefreshCustomers();
        }

        partial void OnSelectedStatusFilterChanged(string value)
        {
            RefreshCustomers();
        }

        public void OnNavigatedTo(object? parameter)
        {
            RefreshCustomers();
        }

        [RelayCommand]
        public void RefreshCustomers()
        {
            var customers = _userManagementService.SearchAccounts(UserRole.Customer, SearchKeyword, SelectedStatusFilter);
            DisplayedCustomers.Clear();
            foreach (var c in customers)
            {
                DisplayedCustomers.Add(c);
            }

            TotalDisplayedCount = DisplayedCustomers.Count;
            var allCustomers = _userManagementService.GetUsersByRole(UserRole.Customer);
            ActiveCustomerCount = allCustomers.Count(u => u.IsActive);
            LockedCustomerCount = allCustomers.Count(u => !u.IsActive);
        }

        [RelayCommand]
        private void ClearFilters()
        {
            SearchKeyword = string.Empty;
            SelectedStatusFilter = UserManagementService.AllStatusFilter;
            RefreshCustomers();
        }

        [RelayCommand]
        public void ToggleCustomerLock(User? customer)
        {
            if (customer == null)
            {
                return;
            }

            string actionLabel = customer.IsActive ? "KHÓA" : "MỞ KHÓA";
            bool confirmed = _dialogService.ShowConfirmation(
                $"Xác nhận {actionLabel} tài khoản khách hàng",
                $"Bạn có chắc chắn muốn {actionLabel} tài khoản khách hàng \"{customer.HoTen}\" ({customer.UserCode} — {customer.Username}) không?\n\n" +
                $"Toàn bộ lịch sử đơn hàng của khách hàng vẫn được bảo toàn nguyên vẹn.");

            if (!confirmed)
            {
                return;
            }

            var result = _userManagementService.ToggleAccountLock(
                customer.MaND,
                _authService.CurrentUser?.MaND);

            if (!result.IsSuccess)
            {
                _dialogService.ShowError("Không thể cập nhật trạng thái", result.Message);
                return;
            }

            RefreshCustomers();
            _dialogService.ShowInformation("Cập nhật tài khoản khách hàng", result.Message);
        }

        [RelayCommand]
        public void ResetCustomerPassword(User? customer)
        {
            if (customer == null)
            {
                return;
            }

            bool confirmed = _dialogService.ShowConfirmation(
                "Xác nhận cấp lại mật khẩu (Mô phỏng)",
                $"Bạn có chắc chắn muốn cấp lại mật khẩu tạm thời cho tài khoản khách hàng:\n" +
                $"• Mã KH: {customer.UserCode}\n" +
                $"• Họ tên: {customer.HoTen}\n" +
                $"• Tên đăng nhập: {customer.Username}\n\n" +
                $"Mật khẩu tạm thời mới sẽ được hiển thị một lần sau khi xác nhận.");

            if (!confirmed)
            {
                return;
            }

            var result = _userManagementService.ResetCustomerPassword(customer.MaND);
            if (!result.IsSuccess)
            {
                _dialogService.ShowError("Không thể cấp lại mật khẩu", result.Message);
                return;
            }

            _dialogService.ShowInformation("Cấp lại mật khẩu thành công (Chỉ hiển thị 1 lần)", result.Message);
        }
    }
}
