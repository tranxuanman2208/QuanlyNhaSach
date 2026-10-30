using System.Windows.Controls;
using QuanlyNhaSach.ViewModels.Staff;

namespace QuanlyNhaSach.Views.Staff
{
    public partial class StaffStockOutPage : Page
    {
        public StaffStockOutPage(StaffStockOutViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
