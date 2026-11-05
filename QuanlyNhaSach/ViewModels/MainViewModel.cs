using QuanlyNhaSach.Services;

namespace QuanlyNhaSach.ViewModels
{
    public class MainViewModel : CustomerMainViewModel
    {
        public MainViewModel(
            AuthService authService,
            CartService cartService,
            INavigationService navigationService,
            IDialogService dialogService)
            : base(authService, cartService, navigationService, dialogService)
        {
        }
    }
}
