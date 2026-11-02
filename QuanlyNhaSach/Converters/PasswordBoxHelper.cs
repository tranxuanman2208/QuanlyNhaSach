using System.Windows;
using System.Windows.Controls;

namespace QuanlyNhaSach.Converters
{
    /// <summary>
    /// Attached behavior for synchronizing WPF PasswordBox with a ViewModel string property
    /// in this isolated in-memory mock authentication prototype.
    /// </summary>
    public static class PasswordBoxHelper
    {
        public static readonly DependencyProperty BoundPasswordProperty =
            DependencyProperty.RegisterAttached(
                "BoundPassword",
                typeof(string),
                typeof(PasswordBoxHelper),
                new FrameworkPropertyMetadata(
                    string.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnBoundPasswordChanged));

        private static readonly DependencyProperty IsUpdatingProperty =
            DependencyProperty.RegisterAttached(
                "IsUpdating",
                typeof(bool),
                typeof(PasswordBoxHelper),
                new PropertyMetadata(false));

        public static string GetBoundPassword(DependencyObject obj) =>
            (string)obj.GetValue(BoundPasswordProperty);

        public static void SetBoundPassword(DependencyObject obj, string value) =>
            obj.SetValue(BoundPasswordProperty, value);

        private static bool GetIsUpdating(DependencyObject obj) =>
            (bool)obj.GetValue(IsUpdatingProperty);

        private static void SetIsUpdating(DependencyObject obj, bool value) =>
            obj.SetValue(IsUpdatingProperty, value);

        private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not PasswordBox passwordBox)
            {
                return;
            }

            passwordBox.PasswordChanged -= HandlePasswordChanged;

            string newPassword = e.NewValue as string ?? string.Empty;
            if (!GetIsUpdating(passwordBox) && passwordBox.Password != newPassword)
            {
                passwordBox.Password = newPassword;
            }

            passwordBox.PasswordChanged += HandlePasswordChanged;
        }

        private static void HandlePasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is not PasswordBox passwordBox)
            {
                return;
            }

            SetIsUpdating(passwordBox, true);
            SetBoundPassword(passwordBox, passwordBox.Password);
            SetIsUpdating(passwordBox, false);
        }
    }
}
