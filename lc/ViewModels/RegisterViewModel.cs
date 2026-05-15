using System.Windows.Controls;
using lc.Commands;
using lc.Services.Interfaces;
using lc.ViewModels.Base;

namespace lc.ViewModels;

public sealed class RegisterViewModel : AuthViewModelBase
{
    public AsyncRelayCommand RegisterCommand { get; }
    public RelayCommand      CancelCommand { get; }
    public RelayCommand      NavigateLoginCommand { get; }

    public RegisterViewModel(
        IAuthService        authService,
        IDialogService      dialogService,
        INavigationService  navigationService) : base(authService, dialogService, navigationService)
    {
        RegisterCommand      = new AsyncRelayCommand(RegisterAsync, CanRegister);
        CancelCommand        = new RelayCommand(_ => RequestClose?.Invoke());
        NavigateLoginCommand = new RelayCommand(_ => OpenLogin());
    }

    private bool CanRegister(object? parameter) { return !IsBusy; }

    private async Task RegisterAsync(object? parameter)
    {
        if (parameter is not object[] values || values.Length != 2)
        {
            SetError("Ошибка формы регистрации.");
            return;
        }

        var password = (values[0] as PasswordBox)?.Password ?? string.Empty;
        var confirmPassword = (values[1] as PasswordBox)?.Password ?? string.Empty;

        if (!ValidateCredentials(UserName, password, out var error))
        {
            SetError(error);
            return;
        }

        if (password != confirmPassword)
        {
            SetError("Пароли не совпадают.");
            return;
        }

        try
        {
            IsBusy = true;
            ClearError();

            await AuthService.RegisterAsync(UserName, password);

            RequestClose?.Invoke();
        }
        catch (InvalidOperationException ex) { SetError(ex.Message); }
        catch (Exception) { SetError("Не удалось создать аккаунт."); }
        finally { IsBusy = false; }
    }

    private void OpenLogin()
    {
        RequestClose?.Invoke();
        DialogService.ShowLoginDialog();
    }

    protected override void OnBusyStateChanged()
    {
        RegisterCommand.RaiseCanExecuteChanged();
    }
}