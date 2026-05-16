using System.Windows;
using lc.Services.Interfaces;
using lc.ViewModels;
using lc.Views.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace lc.Services;

public sealed class WindowService : IWindowService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public WindowService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task OpenReaderAsync(int bookId, int? chapterNumber = null)
    {
        var scope = _scopeFactory.CreateScope();

        try
        {
            var vm = ActivatorUtilities.CreateInstance<ReaderViewModel>(
                scope.ServiceProvider,
                bookId,
                chapterNumber);

            await vm.InitializeAsync();

            var window = new ReaderWindow(vm)
            {
                Owner = Application.Current.MainWindow
            };

            window.Closed += (_, _) => scope.Dispose();
            window.Show();
        }
        catch
        {
            scope.Dispose();
            throw;
        }
    }
}