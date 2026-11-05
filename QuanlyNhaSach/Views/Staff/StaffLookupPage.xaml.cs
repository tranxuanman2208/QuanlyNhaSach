using System.Windows.Controls;
using QuanlyNhaSach.ViewModels.Staff;

namespace QuanlyNhaSach.Views.Staff
{
    public partial class StaffLookupPage : Page
    {
        public StaffLookupPage(StaffLookupViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
