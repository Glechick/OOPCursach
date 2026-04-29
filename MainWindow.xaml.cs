using System.Windows;
using SVMKurs.ViewModels;

namespace SVMKurs
{
    public partial class MainWindow : Window
    {
        private MainWindowViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = new MainWindowViewModel();
            DataContext = _viewModel;
        }
    }
}