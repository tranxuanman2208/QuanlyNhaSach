using System.Windows.Controls;
using QuanlyNhaSach.ViewModels.Staff;

namespace QuanlyNhaSach.Views.Staff
{
    public partial class StaffSalesPage : Page
    {
        public StaffSalesPage(StaffSalesViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
