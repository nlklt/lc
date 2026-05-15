using System.Windows.Controls;
using lc.Commands;
using lc.Services.Interfaces;
using lc.ViewModels.Base;

namespace lc.ViewModels;

public sealed class LoginViewModel : AuthViewModelBase
{
    public AsyncRelayCommand LoginCommand { get; }
    public RelayCommand      CancelCommand { get; }
    public RelayCommand      NavigateRegisterCommand { get; }

    public LoginViewModel(
        IAuthService       authService,
        IDialogService     dialogService,
        INavigationService navigationService) : base(authService, dialogService, navigationService)
    {
        LoginCommand            = new AsyncRelayCommand(LoginAsync, CanLogin);
        CancelCommand           = new RelayCommand(_ => RequestClose?.Invoke());
        NavigateRegisterCommand = new RelayCommand(_ => OpenRegister());
    }

    private bool CanLogin(object? parameter) { return !IsBusy; }

    private async Task LoginAsync(object? parameter)
    {
        if (parameter is not PasswordBox passwordBox)
        {
            SetError("Ошибка формы входа.");
            return;
        }

        var password = passwordBox.Password;

        if (!ValidateCredentials(UserName, password, out var error))
        {
            SetError(error);
            return;
        }

        try
        {
            IsBusy = true;
            ClearError();

            var user =
                await AuthService.LoginAsync(UserName, password);

            if (user is null)
            {
                SetError("Неверный логин или пароль.");
                return;
            }

            RequestClose?.Invoke();
        }
        catch (OperationCanceledException ex) { SetError(ex.Message); }
        catch (Exception) { SetError("Не удалось выполнить вход."); }
        finally { IsBusy = false; }
    }

    private void OpenRegister()
    {
        RequestClose?.Invoke();
        DialogService.ShowRegisterDialog();
    }

    protected override void OnBusyStateChanged()
    {
        LoginCommand.RaiseCanExecuteChanged();
    }
}