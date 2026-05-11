using System.Windows.Input;
using lc.Commands;
using lc.Infrastructure;
using lc.Services.Interfaces;
using lc.ViewModels.Base;

namespace lc.ViewModels
{
    public class RegisterViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private readonly INavigationService _navigation;

        private string _userName = string.Empty;
        private string _password = string.Empty;
        private string _confirmPassword = string.Empty;
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

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value);
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
            _authService = ServiceLocator.AuthService;
            _navigation = ServiceLocator.NavigationService;

            RegisterCommand =
                new AsyncRelayCommand(RegisterAsync, CanRegister);

            NavigateLoginCommand =
                new RelayCommand(_ => _navigation.Navigate(new LoginViewModel()));
        }

        private bool CanRegister(object? obj)
        {
            return !IsBusy &&
                   !string.IsNullOrWhiteSpace(UserName) &&
                   !string.IsNullOrWhiteSpace(Password) &&
                   !string.IsNullOrWhiteSpace(ConfirmPassword);
        }

        private async Task RegisterAsync(object? obj)
        {
            try
            {
                IsBusy = true;
                ErrorMessage = string.Empty;

                if (Password != ConfirmPassword)
                {
                    ErrorMessage = "Пароли не совпадают.";
                    return;
                }

                await _authService.RegisterAsync(UserName, Password);

                _navigation.Navigate(new CatalogViewModel());
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