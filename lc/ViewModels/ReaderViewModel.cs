using lc.Infrastructure;
using lc.Models;
using lc.Services;
using lc.Services.Interfaces;
using lc.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Windows.Input;
using static System.Net.Mime.MediaTypeNames;

namespace lc.ViewModels;

public sealed class ReaderViewModel : ViewModelBase
{
    private readonly IReaderService _readerService;
    private readonly IReadingProgressService _readingProgressService;
    private readonly AppState _appState;
    private readonly IDialogService _dialogService;

    private readonly SemaphoreSlim _saveGate = new(1, 1);

    private int _bookId;
    private int? _chapterNumber;
    private bool _isInitialized;
    private bool _isLoading;
    private bool _isSavingProgress;
    private bool _trackInitialOpen;

    private Chapter? _selectedChapter;
    private string _chapterText = string.Empty;
    private string _bookTitle = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _suppressProgressSaving;

    public ReaderViewModel(
        IReaderService readerService,
        IReadingProgressService readingProgressService,
        AppState appState,
        IDialogService dialogService)
    {
        _readerService = readerService ?? throw new ArgumentNullException(nameof(readerService));
        _readingProgressService = readingProgressService ?? throw new ArgumentNullException(nameof(readingProgressService));
        _appState = appState ?? throw new ArgumentNullException(nameof(appState));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
    }

    public void SetParameters(int bookId, int? chapterNumber = null)
    {
        if (bookId <= 0)
            throw new ArgumentOutOfRangeException(nameof(bookId));

        _bookId = bookId;
        _chapterNumber = chapterNumber;
        _trackInitialOpen = !chapterNumber.HasValue;
    }

    public ObservableCollection<Chapter> Chapters { get; } = [];

    public ICommand PreviousChapterCommand { get; }
    public ICommand NextChapterCommand { get; }

    public string BookTitle
    {
        get => _bookTitle;
        set
        {
            if (SetProperty(ref _bookTitle, value))
                OnPropertyChanged(nameof(ChapterTitle));
        }
    }

    public string ChapterText
    {
        get => _chapterText;
        set => SetProperty(ref _chapterText, value);
    }

    public string ChapterTitle =>
        SelectedChapter is null
            ? string.Empty
            : $"Глава {SelectedChapter.ChapterNumber} — {SelectedChapter.Title}";

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsInitialized
    {
        get => _isInitialized;
        set => SetProperty(ref _isInitialized, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public Chapter? SelectedChapter
    {
        get => _selectedChapter;
        set
        {
            if (!SetProperty(ref _selectedChapter, value))
                return;

            OnPropertyChanged(nameof(ChapterTitle));
            ChapterText = value?.Text ?? string.Empty;
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));

            if (!_suppressProgressSaving &&
                IsInitialized &&
                !IsLoading &&
                value is not null &&
                _appState.CurrentUser is not null)
            {
                _ = SaveCurrentChapterStateAsync(value, updateHistory: false);
            }
        }
    }

    public bool CanGoPrevious =>
        !IsLoading &&
        SelectedChapter is not null &&
        Chapters.Count > 0 &&
        Chapters.IndexOf(SelectedChapter) > 0;

    public bool CanGoNext =>
        !IsLoading &&
        SelectedChapter is not null &&
        Chapters.Count > 0 &&
        Chapters.IndexOf(SelectedChapter) >= 0 &&
        Chapters.IndexOf(SelectedChapter) < Chapters.Count - 1;

    public bool IsSavingProgress
    {
        get => _isSavingProgress;
        private set
        {
            if (SetProperty(ref _isSavingProgress, value))
            {
                OnPropertyChanged(nameof(CanGoPrevious));
                OnPropertyChanged(nameof(CanGoNext));
            }
        }
    }

    public async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            IsInitialized = false;
            ErrorMessage = string.Empty;

            var session = await _readerService.OpenAsync(_bookId, _chapterNumber);

            if (session is null)
            {
                ErrorMessage = "Не удалось открыть книгу.";
                await _dialogService.ShowMessageAsync("Ошибка", ErrorMessage);
                return;
            }

            BookTitle = session.Book.Title;

            Chapters.Clear();
            foreach (var chapter in session.Chapters)
                Chapters.Add(chapter);

            _suppressProgressSaving = true;
            try
            {
                SelectedChapter = session.CurrentChapter;
            }
            finally
            {
                _suppressProgressSaving = false;
            }

            IsInitialized = true;

            if (_trackInitialOpen &&
                _appState.CurrentUser is not null &&
                SelectedChapter is not null)
            {
                await SaveCurrentChapterStateAsync(SelectedChapter, updateHistory: true);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await _dialogService.ShowMessageAsync("Ошибка", ex.Message);
        }
        finally
        {
            _suppressProgressSaving = false;
            IsLoading = false;
        }
    }

    private async Task GoToNextChapterAsync()
    {
        if (!CanGoNext || SelectedChapter is null)
            return;

        var index = Chapters.IndexOf(SelectedChapter);
        if (index < 0 || index >= Chapters.Count - 1)
            return;

        SelectedChapter = Chapters[index + 1];
    }

    private async Task GoToPreviousChapterAsync()
    {
        if (!CanGoPrevious || SelectedChapter is null)
            return;

        var index = Chapters.IndexOf(SelectedChapter);
        if (index <= 0)
            return;

        SelectedChapter = Chapters[index - 1];
    }

    private async Task SaveCurrentChapterStateAsync(Chapter chapter, bool updateHistory)
    {
        var user = _appState.CurrentUser;
        if (user is null)
            return;

        var progressPercent = CalculateProgressPercent(chapter);

        await _saveGate.WaitAsync();
        try
        {
            IsSavingProgress = true;

            await _readingProgressService.SaveProgressAsync(
                user.UserId,
                _bookId,
                chapter.ChapterId,
                progressPercent,
                lastPosition: 0);

            if (updateHistory)
                await _readingProgressService.MarkBookOpenedAsync(user.UserId, _bookId);
        }
        catch (Exception ex)
        {
            ErrorMessage = "Не удалось сохранить прогресс чтения.";
            await _dialogService.ShowMessageAsync("Ошибка", ex.Message);
        }
        finally
        {
            IsSavingProgress = false;
            _saveGate.Release();
        }
    }

    private int CalculateProgressPercent(Chapter chapter)
    {
        if (chapter is null || Chapters.Count == 0)
            return 0;

        var index = Chapters.IndexOf(chapter);
        if (index < 0)
            return 0;

        if (Chapters.Count == 1)
            return 100;

        var percent = (int)Math.Round(((index + 1) * 100.0) / Chapters.Count);
        return Math.Clamp(percent, 1, 100);
    }

    public void Dispose()
    {
        _saveGate.Dispose();
    }
}