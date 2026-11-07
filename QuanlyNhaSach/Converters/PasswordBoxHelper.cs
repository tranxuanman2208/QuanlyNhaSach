using System.Windows;
using System.Windows.Controls;

namespace QuanlyNhaSach.Converters
{
    /// <summary>
    /// Attached behavior for synchronizing WPF PasswordBox with a ViewModel string property
    /// in two-way binding.
    /// </summary>
    public static class PasswordBoxHelper
    {
        public static readonly DependencyProperty BoundPasswordProperty =
            DependencyProperty.RegisterAttached(
                "BoundPassword",
                typeof(string),
                typeof(PasswordBoxHelper),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnBoundPasswordChanged));

        private static readonly DependencyProperty IsUpdatingProperty =
            DependencyProperty.RegisterAttached(
                "IsUpdating",
                typeof(bool),
                typeof(PasswordBoxHelper),
                new PropertyMetadata(false));

        private static readonly DependencyProperty HasBoundProperty =
            DependencyProperty.RegisterAttached(
                "HasBound",
                typeof(bool),
                typeof(PasswordBoxHelper),
                new PropertyMetadata(false));

        public static string? GetBoundPassword(DependencyObject obj) =>
            (string?)obj.GetValue(BoundPasswordProperty);

        public static void SetBoundPassword(DependencyObject obj, string? value) =>
            obj.SetValue(BoundPasswordProperty, value);

        private static bool GetIsUpdating(DependencyObject obj) =>
            (bool)obj.GetValue(IsUpdatingProperty);

        private static void SetIsUpdating(DependencyObject obj, bool value) =>
            obj.SetValue(IsUpdatingProperty, value);

        private static bool GetHasBound(DependencyObject obj) =>
            (bool)obj.GetValue(HasBoundProperty);

        private static void SetHasBound(DependencyObject obj, bool value) =>
            obj.SetValue(HasBoundProperty, value);

        private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not PasswordBox passwordBox)
            {
                return;
            }

            // Đảm bảo chỉ đăng ký sự kiện PasswordChanged một lần duy nhất cho PasswordBox
            if (!GetHasBound(passwordBox))
            {
                SetHasBound(passwordBox, true);
                passwordBox.PasswordChanged += HandlePasswordChanged;
                passwordBox.Loaded += (s, _) =>
                {
                    if (s is PasswordBox pb && !GetIsUpdating(pb))
                    {
                        string currentBound = GetBoundPassword(pb) ?? string.Empty;
                        if (pb.Password != currentBound)
                        {
                            pb.Password = currentBound;
                        }
                    }
                };
            }

            string newPassword = e.NewValue as string ?? string.Empty;
            if (!GetIsUpdating(passwordBox) && passwordBox.Password != newPassword)
            {
                SetIsUpdating(passwordBox, true);
                passwordBox.Password = newPassword;
                SetIsUpdating(passwordBox, false);
            }
        }

        private static void HandlePasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is not PasswordBox passwordBox)
            {
                return;
            }

            if (GetIsUpdating(passwordBox))
            {
                return;
            }

            SetIsUpdating(passwordBox, true);
            SetBoundPassword(passwordBox, passwordBox.Password);
            SetIsUpdating(passwordBox, false);
        }
    }
}
