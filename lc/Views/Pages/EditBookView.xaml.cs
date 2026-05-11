using lc.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace lc.Views.Pages
{
    public partial class EditBookView : UserControl
    {
        public EditBookView()
        {
            InitializeComponent();
            Loaded += EditBookView_Loaded;
        }

        private async void EditBookView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is EditBookViewModel vm)
                await vm.InitializeAsync();
        }
    }
}