using System.Windows;
using SVMKurs.ViewModels;

namespace SVMKurs
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        private void DataManagementView_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}