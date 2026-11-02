using System.Windows.Controls;
using QuanlyNhaSach.ViewModels.Customer;

namespace QuanlyNhaSach.Views.Customer
{
    public partial class CustomerProfilePage : Page
    {
        public CustomerProfilePage()
        {
            InitializeComponent();
            DataContext = new CustomerProfileViewModel();
        }

        public CustomerProfilePage(CustomerProfileViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
