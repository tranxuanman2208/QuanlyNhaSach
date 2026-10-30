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
    public partial class StaffManagementViewModel : ObservableObject, INavigationAware
    {
        private readonly UserManagementService _userManagementService;
        private readonly AuthService _authService;
        private readonly MockDataStore _dataStore;
        private readonly IDialogService _dialogService;

        public ObservableCollection<User> DisplayedStaff { get; } = new();

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
        private int _activeStaffCount;

        [ObservableProperty]
        private int _lockedStaffCount;

        // ==================== ADD STAFF DIALOG STATE ====================
        [ObservableProperty]
        private bool _isAddStaffDialogOpen;

        [ObservableProperty]
        private string _formHoTen = string.Empty;

        [ObservableProperty]
        private string _formSDT = string.Empty;

        [ObservableProperty]
        private string _formDiaChi = string.Empty;

        [ObservableProperty]
        private string _formUsername = string.Empty;

        [ObservableProperty]
        private string _formPassword = string.Empty;

        [ObservableProperty]
        private string _formConfirmPassword = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasFormError))]
        private string _formErrorMessage = string.Empty;

        public bool HasFormError => !string.IsNullOrWhiteSpace(FormErrorMessage);

        public StaffManagementViewModel(
            UserManagementService userManagementService,
            AuthService authService,
            MockDataStore dataStore,
            IDialogService dialogService)
        {
            _userManagementService = userManagementService ?? throw new ArgumentNullException(nameof(userManagementService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            _dataStore.DataChanged += (_, _) => RefreshStaff();
            _userManagementService.UsersChanged += (_, _) => RefreshStaff();

            RefreshStaff();
        }

        partial void OnSearchKeywordChanged(string value)
        {
            RefreshStaff();
        }

        partial void OnSelectedStatusFilterChanged(string value)
        {
            RefreshStaff();
        }

        public void OnNavigatedTo(object? parameter)
        {
            IsAddStaffDialogOpen = false;
            RefreshStaff();
        }

        [RelayCommand]
        public void RefreshStaff()
        {
            var staffList = _userManagementService.SearchAccounts(UserRole.Staff, SearchKeyword, SelectedStatusFilter);
            DisplayedStaff.Clear();
            foreach (var s in staffList)
            {
                DisplayedStaff.Add(s);
            }

            TotalDisplayedCount = DisplayedStaff.Count;
            var allStaff = _userManagementService.GetUsersByRole(UserRole.Staff);
            ActiveStaffCount = allStaff.Count(u => u.IsActive);
            LockedStaffCount = allStaff.Count(u => !u.IsActive);
        }

        [RelayCommand]
        private void ClearFilters()
        {
            SearchKeyword = string.Empty;
            SelectedStatusFilter = UserManagementService.AllStatusFilter;
            RefreshStaff();
        }

        [RelayCommand]
        public void OpenAddStaffDialog()
        {
            FormHoTen = string.Empty;
            FormSDT = string.Empty;
            FormDiaChi = string.Empty;
            FormUsername = string.Empty;
            FormPassword = string.Empty;
            FormConfirmPassword = string.Empty;
            FormErrorMessage = string.Empty;
            IsAddStaffDialogOpen = true;
        }

        [RelayCommand]
        public void CloseAddStaffDialog()
        {
            IsAddStaffDialogOpen = false;
            FormErrorMessage = string.Empty;
        }

        [RelayCommand]
        public void SaveNewStaff()
        {
            FormErrorMessage = string.Empty;

            var result = _userManagementService.AddStaffAccount(
                FormHoTen,
                FormSDT,
                FormDiaChi,
                FormUsername,
                FormPassword,
                FormConfirmPassword);

            if (!result.IsSuccess)
            {
                FormErrorMessage = result.Message;
                return;
            }

            IsAddStaffDialogOpen = false;
            RefreshStaff();
            _dialogService.ShowInformation("Thêm nhân viên thành công (UC02)", result.Message);
        }

        [RelayCommand]
        public void ToggleStaffLock(User? staffUser)
        {
            if (staffUser == null)
            {
                return;
            }

            string actionLabel = staffUser.IsActive ? "KHÓA" : "MỞ KHÓA";
            bool confirmed = _dialogService.ShowConfirmation(
                $"Xác nhận {actionLabel} tài khoản nhân viên",
                $"Bạn có chắc chắn muốn {actionLabel} tài khoản nhân viên \"{staffUser.HoTen}\" ({staffUser.UserCode} — {staffUser.Username}) không?");

            if (!confirmed)
            {
                return;
            }

            var result = _userManagementService.ToggleAccountLock(
                staffUser.MaND,
                _authService.CurrentUser?.MaND);

            if (!result.IsSuccess)
            {
                _dialogService.ShowError("Không thể cập nhật trạng thái", result.Message);
                return;
            }

            RefreshStaff();
            _dialogService.ShowInformation("Cập nhật trạng thái nhân viên", result.Message);
        }
    }
}
