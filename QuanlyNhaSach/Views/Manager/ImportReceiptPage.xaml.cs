using System;
using System.Windows.Controls;
using QuanlyNhaSach.ViewModels.Manager;

namespace QuanlyNhaSach.Views.Manager
{
    public partial class ImportReceiptPage : Page
    {
        public ImportReceiptPage(ImportReceiptViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }
    }
}
