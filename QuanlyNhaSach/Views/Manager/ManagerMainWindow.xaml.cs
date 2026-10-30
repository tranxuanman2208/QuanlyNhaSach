using System;
using System.Windows;
using System.Windows.Controls;
using QuanlyNhaSach.ViewModels.Manager;

namespace QuanlyNhaSach.Views.Manager
{
    public partial class ManagerMainWindow : Window
    {
        private readonly ManagerMainViewModel _viewModel;

        public Frame NavigationFrame => ManagerContentFrame;

        public ManagerMainWindow(ManagerMainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _viewModel.EnsureManagerAuthorized();
        }
    }
}
