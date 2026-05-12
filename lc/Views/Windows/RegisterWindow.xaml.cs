using lc.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace lc.Views.Windows
{
    public partial class RegisterWindow : Window
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

        public RegisterWindow()
        {
            InitializeComponent();

            var vm = new RegisterViewModel();
            vm.RequestClose = this.Close;
            DataContext = vm;
        }
    }
}