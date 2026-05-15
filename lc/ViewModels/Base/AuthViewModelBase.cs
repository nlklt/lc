using System.Text.RegularExpressions;
using lc.Services.Interfaces;

namespace lc.ViewModels.Base;

public abstract class AuthViewModelBase : ViewModelBase
{
    protected readonly IAuthService         AuthService;
    protected readonly IDialogService       DialogService;
    protected readonly INavigationService   NavigationService;

    private string _userName = string.Empty;
    private string _errorMessage = string.Empty;
    private bool   _isBusy;

    protected const int MinUserNameLength = 3;
    protected const int MaxUserNameLength = 16;

    protected const int MinPasswordLength = 6;
    protected const int MaxPasswordLength = 64;

    protected AuthViewModelBase(
        IAuthService        authService,
        IDialogService      dialogService,
        INavigationService  navigationService)
    {
        AuthService         = authService;
        DialogService       = dialogService;
        NavigationService   = navigationService;
    }

    public Action? RequestClose { get; set; }

    public string UserName
    {
        get => _userName;
        set
        {
            var normalized = value?.Trim() ?? string.Empty;

            if (SetProperty(ref _userName, normalized))
            {
                ClearError();
            }
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        protected set => SetProperty(ref _errorMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        protected set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnBusyStateChanged();
            }
        }
    }

    protected virtual void OnBusyStateChanged()
    {
    }

    protected void SetError(string message)
    {
        ErrorMessage = message;
    }

    protected void ClearError()
    {
        ErrorMessage = string.Empty;
    }

    protected bool ValidateCredentials(
        string userName,
        string password,
        out string error)
    {
        userName = userName.Trim();

        if (string.IsNullOrWhiteSpace(userName))
        {
            error = "Введите логин.";
            return false;
        }

        if (userName.Length < MinUserNameLength)
        {
            error = $"Логин должен быть минимум {MinUserNameLength} символа.";
            return false;
        }

        if (userName.Length > MaxUserNameLength)
        {
            error = $"Логин не должен превышать {MaxUserNameLength} символов.";
            return false;
        }

        if (!Regex.IsMatch(userName, @"^[a-zA-Zа-яА-Я0-9_]+$"))
        {
            error = "Логин может содержать только буквы, цифры и _.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            error = "Введите пароль.";
            return false;
        }

        if (password.Length < MinPasswordLength)
        {
            error = $"Пароль должен быть минимум {MinPasswordLength} символов.";
            return false;
        }

        if (password.Length > MaxPasswordLength)
        {
            error = $"Пароль не должен превышать {MaxPasswordLength} символов.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}