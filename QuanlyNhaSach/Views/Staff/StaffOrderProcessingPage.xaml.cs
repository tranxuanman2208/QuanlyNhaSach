using System.Windows.Controls;
using QuanlyNhaSach.ViewModels.Staff;

namespace QuanlyNhaSach.Views.Staff
{
    public partial class StaffOrderProcessingPage : Page
    {
        public StaffOrderProcessingPage(StaffOrderProcessingViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
