using System.Windows;
using lc.Services.Interfaces;
using lc.Views.Windows;

namespace lc.Services
{
    public class WindowService : IWindowService
    {
        public Task OpenReaderAsync(int bookId, int? chapterId = null)
        {
            var window = new ReaderWindow(bookId, chapterId)
            {
                Owner = Application.Current.MainWindow
            };

            window.Show();
            return Task.CompletedTask;
        }
    }
}