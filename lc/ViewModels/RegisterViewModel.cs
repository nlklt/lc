using lc.Commands;
using lc.Infrastructure;
using lc.Models;
using lc.Services.Interfaces;
using lc.ViewModels.Base;
using System.Windows.Controls;
using System.Windows.Input;

namespace lc.ViewModels
{
    public class RegisterViewModel : ViewModelBase
    {
        private readonly IAuthService   _authService;
        private readonly IDialogService _dialogService;

        private readonly AppState _appState;

        private string _userName     = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _isBusy;

        public Action? RequestClose { get; set; }

        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public ICommand RegisterCommand { get; }
        public ICommand NavigateLoginCommand { get; }

        public RegisterViewModel()
        {

            _appState      = ServiceLocator.AppState;
            _authService   = ServiceLocator.AuthService;
            _dialogService = ServiceLocator.DialogService;

            RegisterCommand      = new AsyncRelayCommand(RegisterAsync, CanRegister);
            NavigateLoginCommand = new RelayCommand(OpenLogin);
        }

        private bool CanRegister(object? obj)
        {
            return !IsBusy && !string.IsNullOrWhiteSpace(UserName);
        }

        private async Task RegisterAsync(object? parameter)
        {
            if (parameter is object[] passwordBoxes && passwordBoxes.Length == 2)
            {
                var passBox = passwordBoxes[0] as PasswordBox;
                var confirmPassBox = passwordBoxes[1] as PasswordBox;

                var password = passBox?.Password;
                var confirmPassword = confirmPassBox?.Password;

                if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
                {
                    ErrorMessage = "Заполните оба поля пароля.";
                    return;
                }

                if (password != confirmPassword)
                {
                    ErrorMessage = "Пароли не совпадают.";
                    return;
                }

                try
                {
                    IsBusy = true;
                    ErrorMessage = string.Empty;

                    await _authService.RegisterAsync(UserName, password);

                    RequestClose?.Invoke();
                }
                catch (Exception ex)
                {
                    ErrorMessage = ex.Message;
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        private void OpenLogin(object? obj)
        {
            RequestClose?.Invoke();
            _dialogService.ShowLoginDialog();
        }
    }
}