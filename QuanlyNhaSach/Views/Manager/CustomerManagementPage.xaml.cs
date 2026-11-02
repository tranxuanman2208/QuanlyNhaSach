using System;
using System.Windows.Controls;
using QuanlyNhaSach.ViewModels.Manager;

namespace QuanlyNhaSach.Views.Manager
{
    public partial class CustomerManagementPage : Page
    {
        public CustomerManagementPage(CustomerManagementViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }
    }
}
