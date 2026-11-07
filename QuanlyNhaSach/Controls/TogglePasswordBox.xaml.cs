using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QuanlyNhaSach.Controls
{
    /// <summary>
    /// Custom PasswordBox control featuring a toggleable eye button to reveal/hide password plain text.
    /// Supports TwoWay DataBinding and Enter command execution.
    /// </summary>
    public partial class TogglePasswordBox : UserControl
    {
        private bool _isSyncing;

        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register(
                nameof(Password),
                typeof(string),
                typeof(TogglePasswordBox),
                new FrameworkPropertyMetadata(
                    string.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnPasswordPropertyChanged));

        public static readonly DependencyProperty IsPasswordVisibleProperty =
            DependencyProperty.Register(
                nameof(IsPasswordVisible),
                typeof(bool),
                typeof(TogglePasswordBox),
                new PropertyMetadata(false, OnIsPasswordVisibleChanged));

        public static readonly DependencyProperty MaxLengthProperty =
            DependencyProperty.Register(
                nameof(MaxLength),
                typeof(int),
                typeof(TogglePasswordBox),
                new PropertyMetadata(0, OnMaxLengthChanged));

        public static readonly DependencyProperty EnterCommandProperty =
            DependencyProperty.Register(
                nameof(EnterCommand),
                typeof(ICommand),
                typeof(TogglePasswordBox),
                new PropertyMetadata(null));

        public string Password
        {
            get => (string)GetValue(PasswordProperty);
            set => SetValue(PasswordProperty, value);
        }

        public bool IsPasswordVisible
        {
            get => (bool)GetValue(IsPasswordVisibleProperty);
            set => SetValue(IsPasswordVisibleProperty, value);
        }

        public int MaxLength
        {
            get => (int)GetValue(MaxLengthProperty);
            set => SetValue(MaxLengthProperty, value);
        }

        public ICommand? EnterCommand
        {
            get => (ICommand?)GetValue(EnterCommandProperty);
            set => SetValue(EnterCommandProperty, value);
        }

        public TogglePasswordBox()
        {
            InitializeComponent();
        }

        private static void OnPasswordPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TogglePasswordBox control)
            {
                if (control._isSyncing) return;
                control._isSyncing = true;
                try
                {
                    string val = e.NewValue as string ?? string.Empty;
                    if (control.PartPasswordBox.Password != val)
                    {
                        control.PartPasswordBox.Password = val;
                    }
                    if (control.PartTextBox.Text != val)
                    {
                        control.PartTextBox.Text = val;
                    }
                }
                finally
                {
                    control._isSyncing = false;
                }
            }
        }

        private static void OnIsPasswordVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TogglePasswordBox control)
            {
                bool isVisible = (bool)e.NewValue;
                if (isVisible)
                {
                    control.PartTextBox.Visibility = Visibility.Visible;
                    control.PartPasswordBox.Visibility = Visibility.Collapsed;
                    control.PartTextBox.Focus();
                    control.PartTextBox.CaretIndex = control.PartTextBox.Text.Length;
                }
                else
                {
                    control.PartPasswordBox.Visibility = Visibility.Visible;
                    control.PartTextBox.Visibility = Visibility.Collapsed;
                    control.PartPasswordBox.Focus();
                }
            }
        }

        private static void OnMaxLengthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TogglePasswordBox control)
            {
                int max = (int)e.NewValue;
                control.PartPasswordBox.MaxLength = max;
                control.PartTextBox.MaxLength = max;
            }
        }

        private void PartPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isSyncing) return;
            _isSyncing = true;
            try
            {
                Password = PartPasswordBox.Password;
                if (PartTextBox.Text != PartPasswordBox.Password)
                {
                    PartTextBox.Text = PartPasswordBox.Password;
                }
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private void PartTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncing) return;
            _isSyncing = true;
            try
            {
                Password = PartTextBox.Text;
                if (PartPasswordBox.Password != PartTextBox.Text)
                {
                    PartPasswordBox.Password = PartTextBox.Text;
                }
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                if (EnterCommand != null && EnterCommand.CanExecute(null))
                {
                    EnterCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }

        public new bool Focus()
        {
            return IsPasswordVisible ? PartTextBox.Focus() : PartPasswordBox.Focus();
        }
    }
}
