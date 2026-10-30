using System.Windows.Controls;
using QuanlyNhaSach.ViewModels.Customer;

namespace QuanlyNhaSach.Views.Customer
{
    public partial class CartCheckoutPage : Page
    {
        public CartCheckoutPage(CartCheckoutViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
