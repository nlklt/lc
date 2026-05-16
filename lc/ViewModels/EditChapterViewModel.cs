using lc.Commands;
using lc.Infrastructure;
using lc.Models;
using lc.Models.Enums;
using lc.Services;
using lc.Services.Interfaces;
using lc.ViewModels.Base;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace lc.ViewModels;

public sealed class EditChapterViewModel : ViewModelBase, IDisposable
{
    private const int MaxTitleLength = 255;
    private const int MaxTextLength = 200_000;

    private readonly IChapterService _chapterService;
    private readonly IBookService _bookService;
    private readonly INavigationService _navigation;
    private readonly AppState _appState;
    private readonly SemaphoreSlim _initGate = new(1, 1);

    private readonly int _bookId;
    private readonly int? _chapterId;

    private Book? _book;
    private Chapter? _chapter;

    private bool _isBusy;
    private bool _isInitialized;
    private bool _isInitializing;
    private bool _isDisposed;
    private string _errorMessage = string.Empty;

    private string _title = string.Empty;
    private string _text = string.Empty;
    private int _chapterNumber;
    private ChapterStatus _status;
    private DateTime _createdAt;
    private DateTime _updatedAt;

    public EditChapterViewModel(
        IChapterService chapterService,
        IBookService bookService,
        INavigationService navigation,
        AppState appState,
        int bookId,
        int? chapterId = null)
    {
        if (bookId <= 0)
            throw new ArgumentOutOfRangeException(nameof(bookId));

        _chapterService = chapterService ?? throw new ArgumentNullException(nameof(chapterService));
        _bookService = bookService ?? throw new ArgumentNullException(nameof(bookService));
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        _appState = appState ?? throw new ArgumentNullException(nameof(appState));

        _bookId = bookId;
        _chapterId = chapterId;

        InitializeCommand = new AsyncRelayCommand(_ => InitializeAsync(), _ => !IsBusy && !_isInitialized);
        SaveDraftCommand = new AsyncRelayCommand(_ => SaveDraftAsync(), _ => CanSaveDraft);
        PublishCommand = new AsyncRelayCommand(_ => PublishAsync(), _ => CanPublish);
        SaveCommand = new AsyncRelayCommand(_ => SaveAsync(), _ => CanSave);
        CancelCommand = new RelayCommand(_ => Cancel(), _ => !IsBusy);

        _ = InitializeAsync();
    }

    public bool IsEditMode => _chapterId.HasValue;

    public bool IsPublishedArchiveEditMode =>
        _chapter is not null &&
        _book?.BookStatus == BookStatus.Archived &&
        _chapter.Status == ChapterStatus.Published;

    public string HeaderText => IsEditMode ? "Редактирование главы" : "Новая глава";

    public string ChapterNumberText =>
        IsEditMode ? ChapterNumber.ToString() : "Будет присвоен автоматически";

    public string StatusText => GetStatusText(Status);

    public string CreatedAtText =>
        CreatedAt == default ? "-" : CreatedAt.ToString("dd.MM.yyyy HH:mm");

    public string UpdatedAtText =>
        UpdatedAt == default ? "-" : UpdatedAt.ToString("dd.MM.yyyy HH:mm");

    public bool ShowDraftActions => _isInitialized && !IsPublishedArchiveEditMode;
    public bool ShowSaveAction => _isInitialized && IsPublishedArchiveEditMode;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
                RefreshCommands();
        }
    }

    public bool IsInitialized => _isInitialized;

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public string Title
    {
        get => _title;
        set
        {
            if (SetProperty(ref _title, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(CanSave));
                OnPropertyChanged(nameof(CanSaveDraft));
                OnPropertyChanged(nameof(CanPublish));
                RefreshCommands();
            }
        }
    }

    public string Text
    {
        get => _text;
        set
        {
            if (SetProperty(ref _text, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(CanSave));
                OnPropertyChanged(nameof(CanSaveDraft));
                OnPropertyChanged(nameof(CanPublish));
                RefreshCommands();
            }
        }
    }

    public int ChapterNumber
    {
        get => _chapterNumber;
        private set => SetProperty(ref _chapterNumber, value);
    }

    public ChapterStatus Status
    {
        get => _status;
        private set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(CanSave));
                OnPropertyChanged(nameof(CanSaveDraft));
                OnPropertyChanged(nameof(CanPublish));
            }
        }
    }

    public DateTime CreatedAt
    {
        get => _createdAt;
        private set => SetProperty(ref _createdAt, value);
    }

    public DateTime UpdatedAt
    {
        get => _updatedAt;
        private set => SetProperty(ref _updatedAt, value);
    }

    public bool CanSaveBase =>
        !IsBusy &&
        _isInitialized &&
        IsAllowedToEdit() &&
        IsValidContent();

    public bool CanSaveDraft =>
        CanSaveBase && !IsPublishedArchiveEditMode;

    public bool CanPublish =>
        CanSaveBase &&
        !IsPublishedArchiveEditMode &&
        (!IsEditMode || Status == ChapterStatus.Draft);

    public bool CanSave =>
        CanSaveBase && IsPublishedArchiveEditMode;

    public ICommand InitializeCommand { get; }
    public ICommand SaveDraftCommand { get; }
    public ICommand PublishCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public async Task InitializeAsync()
    {
        if (_isInitialized || _isInitializing || _isDisposed)
            return;

        await _initGate.WaitAsync();
        try
        {
            if (_isInitialized || _isDisposed)
                return;

            _isInitializing = true;
            IsBusy = true;
            ErrorMessage = string.Empty;

            var book = await _bookService.GetBookByIdAsync(_bookId);
            if (book is null)
            {
                ErrorMessage = "Книга не найдена.";
                return;
            }

            _book = book;

            if (!CanOpenForEditing(book))
            {
                ErrorMessage = "Недостаточно прав.";
                return;
            }

            if (_chapterId.HasValue)
            {
                var chapter = await _chapterService.GetByIdAsync(_chapterId.Value);
                if (chapter is null)
                {
                    ErrorMessage = "Глава не найдена.";
                    return;
                }

                if (chapter.BookId != _bookId)
                {
                    ErrorMessage = "Глава не принадлежит этой книге.";
                    return;
                }

                if (!CanEditThisChapter(book, chapter))
                {
                    ErrorMessage = "Недостаточно прав для редактирования этой главы.";
                    return;
                }

                _chapter = chapter;

                Title = chapter.Title ?? string.Empty;
                Text = chapter.Text ?? string.Empty;
                ChapterNumber = chapter.ChapterNumber;
                Status = chapter.Status;
                CreatedAt = chapter.CreatedAt;
                UpdatedAt = chapter.UpdatedAt;
            }
            else
            {
                _chapter = null;

                Title = string.Empty;
                Text = string.Empty;
                ChapterNumber = 0;
                Status = ChapterStatus.Draft;
                CreatedAt = DateTime.Now;
                UpdatedAt = DateTime.Now;
            }

            _isInitialized = true;
            RaiseAllState();
            RefreshCommands();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Не удалось открыть главу: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            _isInitializing = false;
            _initGate.Release();
        }
    }

    private bool CanOpenForEditing(Book book)
    {
        if (_appState.CurrentUser is null)
            return false;

        if (book.BookStatus == BookStatus.Archived)
            return _appState.IsAdmin;

        return _appState.IsAdmin || _appState.CurrentUser.UserId == book.PublisherId;
    }

    private bool CanEditThisChapter(Book book, Chapter chapter)
    {
        if (_appState.CurrentUser is null)
            return false;

        if (book.BookStatus == BookStatus.Archived)
            return _appState.IsAdmin;

        if (chapter.Status != ChapterStatus.Draft)
            return false;

        return _appState.IsAdmin || _appState.CurrentUser.UserId == book.PublisherId;
    }

    private bool IsAllowedToEdit()
    {
        if (_book is null)
            return false;

        if (_appState.CurrentUser is null)
            return false;

        if (_book.BookStatus == BookStatus.Archived)
            return _appState.IsAdmin;

        if (IsEditMode)
        {
            if (_chapter is null)
                return false;

            return _chapter.Status == ChapterStatus.Draft &&
                   (_appState.IsAdmin || _appState.CurrentUser.UserId == _book.PublisherId);
        }

        return _appState.IsAdmin || _appState.CurrentUser.UserId == _book.PublisherId;
    }

    private bool IsValidContent()
    {
        var title = Title.Trim();
        var text = Text.Trim();

        if (string.IsNullOrWhiteSpace(title) || title.Length > MaxTitleLength)
            return false;

        if (string.IsNullOrWhiteSpace(text) || text.Length > MaxTextLength)
            return false;

        return true;
    }

    private async Task SaveDraftAsync()
        => await CommitAsync(ChapterStatus.Draft);

    private async Task PublishAsync()
        => await CommitAsync(ChapterStatus.Published);

    private async Task SaveAsync()
        => await CommitAsync(Status);

    private async Task CommitAsync(ChapterStatus targetStatus)
    {
        if (!CanSaveBase)
            return;

        if (_book is null)
        {
            ErrorMessage = "Книга не загружена.";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var effectiveStatus = IsPublishedArchiveEditMode ? Status : targetStatus;

            var chapter = new Chapter
            {
                ChapterId = _chapterId ?? 0,
                BookId = _bookId,
                ChapterNumber = IsEditMode ? ChapterNumber : 0,
                Title = Title.Trim(),
                Text = Text.Trim(),
                Status = effectiveStatus,
                CreatedAt = CreatedAt == default ? DateTime.Now : CreatedAt,
                UpdatedAt = DateTime.Now
            };

            await _chapterService.SaveAsync(_bookId, chapter, effectiveStatus);
            _navigation.NavigateBack();
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

    private void Cancel()
    {
        _navigation.NavigateBack();
    }

    private void RaiseAllState()
    {
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(IsPublishedArchiveEditMode));
        OnPropertyChanged(nameof(HeaderText));
        OnPropertyChanged(nameof(ChapterNumberText));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(CreatedAtText));
        OnPropertyChanged(nameof(UpdatedAtText));
        OnPropertyChanged(nameof(ShowDraftActions));
        OnPropertyChanged(nameof(ShowSaveAction));
        OnPropertyChanged(nameof(CanSaveBase));
        OnPropertyChanged(nameof(CanSaveDraft));
        OnPropertyChanged(nameof(CanPublish));
        OnPropertyChanged(nameof(CanSave));
    }

    private void RefreshCommands()
    {
        if (SaveDraftCommand is AsyncRelayCommand saveDraft)
            saveDraft.RaiseCanExecuteChanged();

        if (PublishCommand is AsyncRelayCommand publish)
            publish.RaiseCanExecuteChanged();

        if (SaveCommand is AsyncRelayCommand save)
            save.RaiseCanExecuteChanged();

        if (CancelCommand is RelayCommand cancel)
            cancel.RaiseCanExecuteChanged();
    }

    private static string GetStatusText(ChapterStatus status) => status switch
    {
        ChapterStatus.Draft => "Черновик",
        ChapterStatus.Published => "Опубликована",
        ChapterStatus.Archived => "В архиве",
        _ => status.ToString()
    };

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _initGate.Dispose();
    }
}