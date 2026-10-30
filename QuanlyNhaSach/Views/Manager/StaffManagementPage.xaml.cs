using System;
using System.Windows.Controls;
using QuanlyNhaSach.ViewModels.Manager;

namespace QuanlyNhaSach.Views.Manager
{
    public partial class StaffManagementPage : Page
    {
        public StaffManagementPage(StaffManagementViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }
    }
}
