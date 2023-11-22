using System;
using System.ComponentModel;
using System.Windows;
using WpfApp_FindAndCalculateFilesInEachCatalog.ViewModels;

namespace WpfApp_FindAndCalculateFilesInEachCatalog
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel _mainViewModel;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            _mainViewModel.Search();
        }

        private void PauseResumeButton_Click(object sender, RoutedEventArgs e)
        {
            _mainViewModel.PauseOrResume();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            _mainViewModel.Reset();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            _mainViewModel.Dispose();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            if (DataContext == null)
            {
                DataContext = _mainViewModel ??= new MainViewModel();
            }
        }
    }
}
