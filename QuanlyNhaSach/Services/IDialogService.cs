namespace QuanlyNhaSach.Services
{
    public interface IDialogService
    {
        void ShowError(string title, string message);
        void ShowInformation(string title, string message);
        bool ShowConfirmation(string title, string message);
    }
}
