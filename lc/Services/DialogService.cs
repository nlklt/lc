using lc.Services.Interfaces;
using System.Windows;

namespace lc.Services
{
    public class DialogService : IDialogService
    {
        public Task ShowMessageAsync(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            return Task.CompletedTask;
        }

        public Task<bool> ShowConfirmAsync(string title, string message) 
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.OKCancel, MessageBoxImage.Question);
            return Task.FromResult(result == MessageBoxResult.OK);
        }

        public Task<string?> ChooseListAsync(string title, string message, IReadOnlyList<string> options)
        {
            throw new NotImplementedException();
        }
    }
}