using System.Windows;
using System.Windows.Controls;
using QuanlyNhaSach.ViewModels;

namespace QuanlyNhaSach.Views.Customer
{
    public partial class CustomerMainWindow : Window
    {
        public Frame NavigationFrame => MainFrame;

        public CustomerMainWindow()
        {
            InitializeComponent();
        }

        public CustomerMainWindow(CustomerMainViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}
