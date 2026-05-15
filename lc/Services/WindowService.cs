using System.Windows;
using lc.Services.Interfaces;
using lc.Views.Windows;
using lc.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace lc.Services
{
    public class WindowService : IWindowService
    {
        private readonly IServiceProvider _provider;

        public WindowService(IServiceProvider provider)
        {
            _provider = provider;
        }

        public async Task OpenReaderAsync(int bookId, int? chapterId = null)
        {
            var window = _provider.GetRequiredService<ReaderWindow>();
            window.Owner = Application.Current.MainWindow;

            if (window.DataContext is ReaderViewModel vm)
            {
                await vm.InitializeAsync(bookId, chapterId);
            }

            window.Show();
        }
    }
}