using System;
using System.Windows.Controls;
using QuanlyNhaSach.ViewModels.Manager;

namespace QuanlyNhaSach.Views.Manager
{
    public partial class ImportHistoryPage : Page
    {
        public ImportHistoryPage(ImportHistoryViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }
    }
}
