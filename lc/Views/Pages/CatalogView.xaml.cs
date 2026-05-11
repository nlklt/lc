using lc.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace lc.Views.Pages
{
    /// <summary>
    /// Логика взаимодействия для CatalogView.xaml
    /// </summary>
    public partial class CatalogView : UserControl
    {
        public CatalogView()
        {
            Loaded += CatalogView_Loaded;
            InitializeComponent();
        }
        private async void CatalogView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is CatalogViewModel vm)
                await vm.InitializeAsync();
        }
    }
}
