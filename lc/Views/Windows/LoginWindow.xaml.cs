using System.Windows;
using lc.ViewModels;

namespace lc.Views.Windows
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();

            var vm = new LoginViewModel();
            vm.RequestClose = this.Close;
            DataContext = vm;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}