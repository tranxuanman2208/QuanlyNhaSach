using QuanlyNhaSach.Services;

namespace QuanlyNhaSach.ViewModels
{
    public class MainViewModel : CustomerMainViewModel
    {
        public MainViewModel(
            AuthService authService,
            CartService cartService,
            INavigationService navigationService,
            IDialogService dialogService,
            BookService? bookService = null)
            : base(authService, cartService, navigationService, dialogService, bookService)
        {
        }
    }
}
