using lc.Commands;
using lc.Helpers;
using lc.Services.Interfaces;
using lc.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace lc.ViewModels;

public sealed class AdminUsersViewModel : ViewModelBase
{
    private readonly IAdminUserService _adminUserService;
    private readonly IDialogService _dialogService;

    private bool _isLoading;
    private string _errorMessage = string.Empty;

    public AdminUsersViewModel(
        IAdminUserService adminUserService,
        IDialogService dialogService)
    {
        _adminUserService = adminUserService ?? throw new ArgumentNullException(nameof(adminUserService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        ReloadCommand = new AsyncRelayCommand(_ => ReloadAsync(), _ => !IsLoading);
        ToggleCommentBlockCommand = new AsyncRelayCommand(ToggleCommentBlockAsync, _ => !IsLoading);
        DeleteUserCommand = new AsyncRelayCommand(DeleteUserAsync, _ => !IsLoading);

        _ = ReloadAsync();
    }

    public ObservableCollection<AdminUserRowDto> Users { get; } = [];

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                (ReloadCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (ToggleCommentBlockCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (DeleteUserCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public ICommand ReloadCommand { get; }
    public ICommand ToggleCommentBlockCommand { get; }
    public ICommand DeleteUserCommand { get; }

    public async Task ReloadAsync()
    {
        if (IsLoading)
            return;

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var users = await _adminUserService.GetUsersAsync();

            Users.Clear();
            foreach (var user in users)
                Users.Add(user);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await _dialogService.ShowMessageAsync("Ошибка", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ToggleCommentBlockAsync(object? parameter)
    {
        if (parameter is not AdminUserRowDto user)
            return;

        try
        {
            await _adminUserService.ToggleCommentBlockAsync(user.UserId);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("Ошибка", ex.Message);
        }
    }

    private async Task DeleteUserAsync(object? parameter)
    {
        if (parameter is not AdminUserRowDto user)
            return;

        if (!user.CanDelete)
        {
            await _dialogService.ShowMessageAsync("Ошибка", "Этого пользователя удалить нельзя.");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmAsync(
            "Удалить пользователя",
            $"Удалить пользователя «{user.UserName}»?");

        if (!confirmed)
            return;

        try
        {
            await _adminUserService.DeleteUserAsync(user.UserId);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("Ошибка", ex.Message);
        }
    }
}