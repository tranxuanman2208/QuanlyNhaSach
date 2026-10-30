using System.Windows.Controls;
using QuanlyNhaSach.ViewModels.Customer;

namespace QuanlyNhaSach.Views.Customer
{
    public partial class HomePage : Page
    {
        public HomePage(HomeViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
