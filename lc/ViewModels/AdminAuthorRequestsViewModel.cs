using lc.Commands;
using lc.Infrastructure;
using lc.Models;
using lc.Models.Enums;
using lc.Services.Interfaces;
using lc.ViewModels.Base;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace lc.ViewModels;

public sealed class AdminAuthorRequestsViewModel : ViewModelBase, IDisposable
{
    private readonly IAuthorRequestService _authorRequestService;
    private readonly AppState _appState;
    private readonly SemaphoreSlim _loadGate = new(1, 1);

    private bool _isBusy;
    private string _statusMessage = string.Empty;
    private AuthorRequest? _selectedRequest;
    private string _reviewComment = string.Empty;
    private bool _isDisposed;
    private int _loadVersion;
    private bool IsAdmin => _appState.IsAdmin;

    public AdminAuthorRequestsViewModel(
        IAuthorRequestService authorRequestService,
        AppState appState)
    {
        _authorRequestService = authorRequestService ?? throw new ArgumentNullException(nameof(authorRequestService));
        _appState = appState ?? throw new ArgumentNullException(nameof(appState));

        RefreshCommand = new AsyncRelayCommand(_ => LoadAsync(), _ => !IsBusy);
        ApproveCommand = new AsyncRelayCommand(ApproveAsync, CanReviewRequest);
        RejectCommand = new AsyncRelayCommand(RejectAsync, CanReviewRequest);

        _appState.PropertyChanged += OnAppStatePropertyChanged;

        _ = LoadAsync();
    }

    public ObservableCollection<AuthorRequest> Requests { get; } = [];

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
                RaiseCommands();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public AuthorRequest? SelectedRequest
    {
        get => _selectedRequest;
        set
        {
            if (SetProperty(ref _selectedRequest, value))
            {
                ReviewComment = value?.ReviewComment ?? string.Empty;
                RaiseCommands();
                OnPropertyChanged(nameof(SelectedRequestUserName));
                OnPropertyChanged(nameof(SelectedRequestMessage));
                OnPropertyChanged(nameof(SelectedRequestCreatedAtText));
                OnPropertyChanged(nameof(SelectedRequestReviewedAtText));
                OnPropertyChanged(nameof(SelectedRequestReviewerText));
                OnPropertyChanged(nameof(SelectedRequestReviewCommentText));
            }
        }
    }

    public string ReviewComment
    {
        get => _reviewComment;
        set => SetProperty(ref _reviewComment, value);
    }

    public string SelectedRequestUserName =>
        SelectedRequest?.User?.UserName ?? "Выберите заявку";

    public string SelectedRequestMessage =>
        SelectedRequest?.Message ?? " ";

    public string SelectedRequestCreatedAtText =>
        SelectedRequest is null
            ? string.Empty
            : $"Отправлена: {SelectedRequest.CreatedAt:dd.MM.yyyy HH:mm}";

    public string SelectedRequestReviewedAtText =>
        SelectedRequest?.ReviewedAt is null
            ? "Рассмотрена: —"
            : $"Рассмотрена: {SelectedRequest.ReviewedAt:dd.MM.yyyy HH:mm}";

    public string SelectedRequestReviewerText =>
        string.IsNullOrWhiteSpace(SelectedRequest?.Reviewer?.UserName)
            ? "Рассмотрел: —"
            : $"Рассмотрел: {SelectedRequest.Reviewer.UserName}";

    public string SelectedRequestReviewCommentText =>
        string.IsNullOrWhiteSpace(SelectedRequest?.ReviewComment)
            ? "Комментарий: —"
            : $"Комментарий: {SelectedRequest.ReviewComment}";

    public ICommand RefreshCommand { get; }
    public ICommand ApproveCommand { get; }
    public ICommand RejectCommand { get; }

    private async Task LoadAsync(object? _ = null)
    {
        var version = Interlocked.Increment(ref _loadVersion);

        await _loadGate.WaitAsync();
        try
        {
            IsBusy = true;
            StatusMessage = string.Empty;

            if (!_appState.IsAdmin)
            {
                Requests.Clear();
                SelectedRequest = null;
                StatusMessage = "Требуются права администратора.";
                return;
            }

            var requests = (await _authorRequestService.GetPendingRequestsAsync())
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            if (version != Volatile.Read(ref _loadVersion))
                return;

            Requests.Clear();
            foreach (var request in requests)
                Requests.Add(request);

            SelectedRequest = Requests.FirstOrDefault();
            StatusMessage = Requests.Count == 0
                ? "Заявок пока нет."
                : $"Загружено заявок: {Requests.Count}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка загрузки: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            _loadGate.Release();
            RaiseCommands();
        }
    }

    private bool CanReviewRequest(object? _)
        => _appState.IsAdmin
           && !IsBusy
           && SelectedRequest is { Status: AuthorRequestStatus.Pending };

    private async Task ApproveAsync(object? _)
    {
        if (SelectedRequest is null || _appState.CurrentUser is null)
            return;

        try
        {
            IsBusy = true;

            await _authorRequestService.ApproveAsync(
                SelectedRequest.RequestId,
                _appState.CurrentUser.UserId,
                string.IsNullOrWhiteSpace(ReviewComment) ? null : ReviewComment.Trim());

            await LoadAsync();
            StatusMessage = "Заявка одобрена.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка подтверждения: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RejectAsync(object? _)
    {
        if (SelectedRequest is null || _appState.CurrentUser is null)
            return;

        try
        {
            IsBusy = true;

            await _authorRequestService.RejectAsync(
                SelectedRequest.RequestId,
                _appState.CurrentUser.UserId,
                string.IsNullOrWhiteSpace(ReviewComment) ? null : ReviewComment.Trim());

            await LoadAsync();
            StatusMessage = "Заявка отклонена.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка отклонения: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnAppStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AppState.CurrentUser) or nameof(AppState.IsAdmin))
            _ = LoadAsync();
    }

    private void RaiseCommands()
    {
        (RefreshCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (ApproveCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (RejectCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _appState.PropertyChanged -= OnAppStatePropertyChanged;
        _loadGate.Dispose();
    }
}