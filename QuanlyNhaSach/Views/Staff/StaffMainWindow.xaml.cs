using System.Windows;
using System.Windows.Controls;
using QuanlyNhaSach.ViewModels;

namespace QuanlyNhaSach.Views.Staff
{
    public partial class StaffMainWindow : Window
    {
        public Frame NavigationFrame => StaffContentFrame;

        public StaffMainWindow()
        {
            InitializeComponent();
        }

        public StaffMainWindow(StaffMainViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}
