using lc.Services.Interfaces;
using lc.ViewModels;
using lc.Views.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System;
using System.Windows;

namespace lc.Services
{
    public class DialogService : IDialogService
    {
        private readonly IServiceProvider _provider;

        public DialogService(IServiceProvider provider)
        {
            _provider = provider;
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

        public async Task<string?> ShowInputAsync(string title, string message, string placeholder = "")
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var dialog = _provider.GetRequiredService<InputDialog>();

                if (dialog.DataContext is InputViewModel vm)
                {
                    vm.Title = title;
                    vm.Message = message;

                    dialog.Owner = Application.Current.MainWindow;

                    vm.RequestClose = (result) =>
                    {
                        try
                        {
                            dialog.DialogResult = result;
                        }
                        catch { }
                        dialog.Close();
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        return vm.InputText;
                    }
                }

                return null;
            });
        }

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

        public async Task<byte?> ShowRatingAsync(string title, string message, byte initialRating = 0)
        {
            return await Application.Current.Dispatcher.InvokeAsync<byte?>(() =>
            {
                var dialog = _provider.GetRequiredService<RatingDialog>();

                if (dialog.DataContext is RatingViewModel vm)
                {
                    vm.Title = title;
                    vm.Message = message;
                    vm.SelectedRating = initialRating is >= 1 and <= 5 ? initialRating : (byte)0;

                    dialog.Owner = Application.Current.MainWindow;

                    vm.RequestClose = result =>
                    {
                        try
                        {
                            dialog.DialogResult = result;
                        }
                        catch { }

                        dialog.Close();
                    };

                    return dialog.ShowDialog() == true ? vm.SelectedRating : null;
                }

                return null;
            });
        }

        public bool? ShowLoginDialog()
        {
            var loginWindow = _provider.GetRequiredService<LoginWindow>();
            return loginWindow.ShowDialog();
        }

        public bool? ShowRegisterDialog()
        {
            var registerWindow = _provider.GetRequiredService<RegisterWindow>();
            return registerWindow.ShowDialog();
        }
    }
}