using System.Windows;
using lc.ViewModels;

namespace lc.Views.Windows
{
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();

            var vm = new RegisterViewModel();
            vm.RequestClose = this.Close;
            DataContext = vm;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}