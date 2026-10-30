using System.Windows.Controls;
using QuanlyNhaSach.ViewModels.Customer;

namespace QuanlyNhaSach.Views.Customer
{
    public partial class BookDetailPage : Page
    {
        public BookDetailPage(BookDetailViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
