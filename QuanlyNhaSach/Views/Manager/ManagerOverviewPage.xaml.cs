using System;
using System.Windows.Controls;
using QuanlyNhaSach.ViewModels.Manager;

namespace QuanlyNhaSach.Views.Manager
{
    public partial class ManagerOverviewPage : Page
    {
        public ManagerOverviewPage(ManagerOverviewViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }
    }
}
