using System.Windows;
using lc.ViewModels;

namespace lc.Views.Windows
{
    public partial class ReaderWindow : Window
    {
        public ReaderWindow(int bookId, int? chapterId = null)
        {
            DataContext = new ReaderViewModel(bookId, chapterId);
            InitializeComponent();
        }
    }
}