using lc.Services.Interfaces;
using lc.Views.Windows;
using Microsoft.Win32;
using System.Windows;

namespace lc.Services
{
    public class DialogService : IDialogService
    {
        public string? OpenFile(string title, string filter)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = title,
                Filter = filter
            };

            if (openFileDialog.ShowDialog() == true)
            {
                return openFileDialog.FileName;
            }
            return null;
        }

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


        public bool? ShowLoginDialog()
        {
            var loginWindow = new LoginWindow();
            return loginWindow.ShowDialog();
        }

        public bool? ShowRegisterDialog()
        {
            var registerWindow = new RegisterWindow();
            return registerWindow.ShowDialog();
        }
    }
}