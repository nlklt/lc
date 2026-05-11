using lc.Services.Interfaces;
using System.Windows;

namespace lc.Services
{
    public class DialogService : IDialogService
    {
        public async Task<bool> ConfirmAsync(string title, string message) 
        { return true; }
        public async Task<string?> ChooseListAsync(string title, string message, IReadOnlyList<string> options) 
        { return ""; }
        public Task ShowMessageAsync(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            return Task.CompletedTask;
        }
    }
}