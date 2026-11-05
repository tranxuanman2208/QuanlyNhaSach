using System.Windows;
using System.Windows.Controls;
using QuanlyNhaSach.ViewModels;

namespace QuanlyNhaSach
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Frame NavigationFrame => MainFrame;

        public MainWindow()
        {
            InitializeComponent();
        }

        public MainWindow(MainViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}
