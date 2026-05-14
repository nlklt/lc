using lc.ViewModels;
using System.Windows;

namespace lc.Views.Windows
{
    public partial class LoginWindow : Window
    {
        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal;
            else
                WindowState = WindowState.Maximized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public LoginWindow()
        {
            InitializeComponent();

            var vm = new LoginViewModel();
            vm.RequestClose = this.Close;
            DataContext = vm;
        }
    }
}