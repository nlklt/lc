using System.Windows.Input;
using lc.Commands;
using lc.Infrastructure;
using lc.Services.Interfaces;
using lc.ViewModels.Base;

namespace lc.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private readonly INavigationService _navigation;

        private string _userName = string.Empty;
        private string _password = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _isBusy;

        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
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
            _authService = ServiceLocator.AuthService;
            _navigation = ServiceLocator.NavigationService;

            LoginCommand = new AsyncRelayCommand(LoginAsync, CanLogin);
            NavigateRegisterCommand =
                new RelayCommand(_ => _navigation.Navigate(new RegisterViewModel()));
        }

        private bool CanLogin(object? obj)
        {
            return !IsBusy &&
                   !string.IsNullOrWhiteSpace(UserName) &&
                   !string.IsNullOrWhiteSpace(Password);
        }

        private async Task LoginAsync(object? obj)
        {
            try
            {
                IsBusy = true;
                ErrorMessage = string.Empty;

                var user = await _authService.LoginAsync(UserName, Password);

                if (user == null)
                {
                    ErrorMessage = "Неверный логин или пароль.";
                    return;
                }

                _navigation.Navigate(new ProfileViewModel());
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
}