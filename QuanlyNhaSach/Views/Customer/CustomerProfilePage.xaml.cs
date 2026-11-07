using System.Windows;
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

        private void BtnJoinCalendar_Click(object sender, RoutedEventArgs e)
        {
            JoinCalendarPopup.IsOpen = !JoinCalendarPopup.IsOpen;
        }

        private void JoinCalendarControl_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            JoinCalendarPopup.IsOpen = false;
        }
    }
}
