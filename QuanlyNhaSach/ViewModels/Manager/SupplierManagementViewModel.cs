using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanlyNhaSach.Models;
using QuanlyNhaSach.Navigation;
using QuanlyNhaSach.Services;

namespace QuanlyNhaSach.ViewModels.Manager
{
    public partial class SupplierManagementViewModel : ObservableObject, INavigationAware
    {
        private readonly SupplierService _supplierService;
        private readonly IDialogService _dialogService;

        public ObservableCollection<NHACUNGCAP> DisplayedSuppliers { get; } = new();

        [ObservableProperty]
        private string _searchKeyword = string.Empty;

        [ObservableProperty]
        private int _totalDisplayedCount;

        // ==================== ADD / EDIT SUPPLIER DIALOG STATE ====================
        [ObservableProperty]
        private bool _isSupplierDialogOpen;

        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private string _editingSupplierId = string.Empty;

        [ObservableProperty]
        private string _dialogTitle = "Thêm nhà cung cấp mới";

        [ObservableProperty]
        private string _formTenNCC = string.Empty;

        [ObservableProperty]
        private string _formSDT = string.Empty;

        [ObservableProperty]
        private string _formEmail = string.Empty;

        [ObservableProperty]
        private string _formDiaChi = string.Empty;

        [ObservableProperty]
        private bool _formIsActive = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasFormError))]
        private string _formErrorMessage = string.Empty;

        public bool HasFormError => !string.IsNullOrWhiteSpace(FormErrorMessage);

        public SupplierManagementViewModel(
            SupplierService supplierService,
            IDialogService dialogService)
        {
            _supplierService = supplierService ?? throw new ArgumentNullException(nameof(supplierService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            _supplierService.SuppliersChanged += (_, _) => RefreshSuppliers();

            RefreshSuppliers();
        }

        partial void OnSearchKeywordChanged(string value)
        {
            RefreshSuppliers();
        }

        public void OnNavigatedTo(object? parameter)
        {
            IsSupplierDialogOpen = false;
            RefreshSuppliers();
        }

        [RelayCommand]
        public void RefreshSuppliers()
        {
            var list = _supplierService.SearchSuppliers(SearchKeyword, includeInactive: true);
            DisplayedSuppliers.Clear();
            foreach (var s in list)
            {
                DisplayedSuppliers.Add(s);
            }

            TotalDisplayedCount = DisplayedSuppliers.Count;
        }

        [RelayCommand]
        private void ClearSearch()
        {
            SearchKeyword = string.Empty;
            RefreshSuppliers();
        }

        [RelayCommand]
        public void OpenAddSupplierDialog()
        {
            IsEditMode = false;
            EditingSupplierId = string.Empty;
            DialogTitle = "Thêm nhà cung cấp mới (UC12)";
            FormTenNCC = string.Empty;
            FormSDT = string.Empty;
            FormEmail = string.Empty;
            FormDiaChi = string.Empty;
            FormIsActive = true;
            FormErrorMessage = string.Empty;
            IsSupplierDialogOpen = true;
        }

        [RelayCommand]
        public void OpenEditSupplierDialog(NHACUNGCAP? supplier)
        {
            if (supplier == null)
            {
                return;
            }

            IsEditMode = true;
            EditingSupplierId = supplier.MaNCC;
            DialogTitle = $"Chỉnh sửa nhà cung cấp {supplier.SupplierCode}";
            FormTenNCC = supplier.TenNCC ?? string.Empty;
            FormSDT = supplier.SDT ?? string.Empty;
            FormEmail = supplier.Email ?? string.Empty;
            FormDiaChi = supplier.DiaChi;
            FormIsActive = supplier.IsActive;
            FormErrorMessage = string.Empty;
            IsSupplierDialogOpen = true;
        }

        [RelayCommand]
        public void CloseSupplierDialog()
        {
            IsSupplierDialogOpen = false;
            FormErrorMessage = string.Empty;
        }

        [RelayCommand]
        public void SaveSupplier()
        {
            FormErrorMessage = string.Empty;

            OperationResult<NHACUNGCAP> result = IsEditMode
                ? _supplierService.UpdateSupplier(
                    EditingSupplierId,
                    FormTenNCC,
                    FormSDT,
                    FormEmail,
                    FormDiaChi,
                    FormIsActive)
                : _supplierService.AddSupplier(
                    FormTenNCC,
                    FormSDT,
                    FormEmail,
                    FormDiaChi);

            if (!result.IsSuccess)
            {
                FormErrorMessage = result.Message;
                return;
            }

            IsSupplierDialogOpen = false;
            RefreshSuppliers();
            _dialogService.ShowInformation("Quản lý Nhà cung cấp (UC12)", result.Message);
        }

        [RelayCommand]
        public void ToggleSupplierStatus(NHACUNGCAP? supplier)
        {
            if (supplier == null)
            {
                return;
            }

            string actionText = supplier.IsActive ? "ngừng hợp tác" : "kích hoạt lại";
            bool confirmed = _dialogService.ShowConfirmation(
                "Xác nhận trạng thái Nhà cung cấp",
                $"Bạn có chắc chắn muốn {actionText} với nhà cung cấp \"{supplier.TenNCC}\" ({supplier.SupplierCode}) không?\n\n" +
                $"Các phiếu nhập hàng lịch sử của nhà cung cấp này vẫn được giữ nguyên.");

            if (!confirmed)
            {
                return;
            }

            var result = _supplierService.ToggleOrDeactivateSupplier(supplier.MaNCC);
            if (!result.IsSuccess)
            {
                _dialogService.ShowError("Không thể thực hiện", result.Message);
                return;
            }

            RefreshSuppliers();
            _dialogService.ShowInformation("Cập nhật Nhà cung cấp", result.Message);
        }
    }
}
