using System.Windows.Controls;
using System.Windows.Input;
using lc.Commands;
using lc.Infrastructure;
using lc.Services.Interfaces;
using lc.ViewModels.Base;

namespace lc.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly IAuthService   _authService;
        private readonly IDialogService _dialogService;

        private readonly AppState _appState;

        private string _userName     = string.Empty;
        private string _errorMessage = string.Empty;
        private bool   _isBusy;

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

        public ICommand LoginCommand { get; }
        public ICommand NavigateRegisterCommand { get; }

        public LoginViewModel()
        {
            _appState      = ServiceLocator.AppState;
            _authService   = ServiceLocator.AuthService;
            _dialogService = ServiceLocator.DialogService;

            LoginCommand            = new AsyncRelayCommand(LoginAsync, CanLogin);
            NavigateRegisterCommand = new RelayCommand(OpenRegister);
        }

        private bool CanLogin(object? obj)
        {
            return !IsBusy && !string.IsNullOrWhiteSpace(UserName);
        }

        private async Task LoginAsync(object? parameter)
        {
            if (parameter is PasswordBox passwordBox)
            {
                var password = passwordBox.Password;

                if (string.IsNullOrWhiteSpace(password))
                {
                    ErrorMessage = "Введите пароль.";
                    return;
                }

                try
                {
                    IsBusy = true;
                    ErrorMessage = string.Empty;

                    var user = await _authService.LoginAsync(UserName, password);

                    if (user == null)
                    {
                        ErrorMessage = "Неверный логин или пароль.";
                        return;
                    }

                    _appState.CurrentUser = user;
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

        private void OpenRegister(object? obj)
        {
            RequestClose?.Invoke();
            _dialogService.ShowRegisterDialog();
        }
    }
}