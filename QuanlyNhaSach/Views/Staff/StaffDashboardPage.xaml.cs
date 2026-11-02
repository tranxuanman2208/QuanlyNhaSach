using System.Windows.Controls;
using QuanlyNhaSach.ViewModels.Staff;

namespace QuanlyNhaSach.Views.Staff
{
    public partial class StaffDashboardPage : Page
    {
        public StaffDashboardPage(StaffDashboardViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
