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
        private readonly IAuthService   _auth;
        private readonly IDialogService _dialog;
        private readonly INavigationService _navigation;

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

        public ICommand CancelCommand { get; }
        public ICommand LoginCommand { get; }
        public ICommand NavigateRegisterCommand { get; }

        public LoginViewModel()
        {

            _appState = ServiceLocator.AppState;

            _auth       = ServiceLocator.AuthService;
            _dialog     = ServiceLocator.DialogService;
            _navigation = ServiceLocator.NavigationService;

            CancelCommand           = new RelayCommand(GoBack);
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

                if (!ValidateLoginData(UserName, password, out var error))
                {
                    ErrorMessage = error;
                    return;
                }

                try
                {
                    IsBusy = true;
                    ErrorMessage = string.Empty;

                    var user = await _auth.LoginAsync(UserName, password);

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

        private const int MaxUserNameLength = 16;
        private const int MaxPasswordLength = 24;
        private bool ValidateLoginData(string userName, string password, out string error)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                error = "Введите логин.";
                return false;
            }

            if (userName.Length > MaxUserNameLength)
            {
                error = $"Логин не должен быть длиннее {MaxUserNameLength} символов.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                error = "Введите пароль.";
                return false;
            }

            if (password.Length > MaxPasswordLength)
            {
                error = $"Пароль не должен быть длиннее {MaxPasswordLength} символов.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private void GoBack(object? obj)
        {
            RequestClose?.Invoke();
            _navigation.Navigate(_appState?.PrevViewModel ?? new CatalogViewModel());

        }

        private void OpenRegister(object? obj)
        {
            RequestClose?.Invoke();
            _dialog.ShowRegisterDialog();
        }
    }
}