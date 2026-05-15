using System.Windows;
using lc.ViewModels;

namespace lc.Views.Windows
{
    public partial class ReaderWindow : Window
    {
        public ReaderWindow(ReaderViewModel vm)
        {
            DataContext = vm;
            InitializeComponent();
        }
    }
}