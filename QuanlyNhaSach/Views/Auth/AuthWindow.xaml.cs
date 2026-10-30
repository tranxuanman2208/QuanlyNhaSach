using System.Windows;
using QuanlyNhaSach.ViewModels;

namespace QuanlyNhaSach.Views.Auth
{
    public partial class AuthWindow : Window
    {
        public AuthWindow()
        {
            InitializeComponent();
        }

        public AuthWindow(AuthViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}
