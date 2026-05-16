using lc.Infrastructure;
using lc.Models;
using lc.Services.Interfaces;
using lc.ViewModels.Base;
using System.Collections.ObjectModel;

namespace lc.ViewModels;

public sealed class ReaderViewModel : ViewModelBase
{
    private readonly IReaderService _readerService;
    private readonly IReadingProgressService _progressService;
    private readonly AppState _appState;
    private readonly IDialogService _dialogService;

    private readonly int _bookId;
    private readonly int? _chapterNumber;

    private Chapter? _selectedChapter;
    private string _chapterText = string.Empty;
    private string _bookTitle = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isInitialized;
    private bool _isLoading;
    private bool _suppressProgressSaving;

    public ReaderViewModel(
        IReaderService readerService,
        IReadingProgressService progressService,
        AppState appState,
        IDialogService dialogService,
        int bookId,
        int? chapterNumber = null)
    {
        _readerService = readerService;
        _progressService = progressService;
        _appState = appState;
        _dialogService = dialogService;
        _bookId = bookId;
        _chapterNumber = chapterNumber;
    }

    public ObservableCollection<Chapter> Chapters { get; } = [];

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
            if (SetProperty(ref _selectedChapter, value))
            {
                OnPropertyChanged(nameof(ChapterTitle));
                ChapterText = value?.Text ?? string.Empty;

                if (!_suppressProgressSaving)
                {
                    _ = SaveProgressAsync();
                }
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
            {
                Chapters.Add(chapter);
            }

            _suppressProgressSaving = true;
            SelectedChapter = session.CurrentChapter;
            await RestoreProgressAsync();
            _suppressProgressSaving = false;

            IsInitialized = true;
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

    private async Task RestoreProgressAsync()
    {
        if (_chapterNumber.HasValue)
            return;

        var user = _appState.CurrentUser;
        if (user is null)
            return;

        var progress = await _progressService.GetLastBookProgressAsync(user.UserId, _bookId);

        if (progress?.Chapter is null)
            return;

        var chapter = Chapters.FirstOrDefault(x => x.ChapterId == progress.ChapterId);

        if (chapter is not null)
        {
            SelectedChapter = chapter;
        }
    }

    private async Task SaveProgressAsync()
    {
        if (SelectedChapter is null)
            return;

        var user = _appState.CurrentUser;
        if (user is null)
            return;

        await _progressService.SaveProgressAsync(
            user.UserId,
            _bookId,
            SelectedChapter.ChapterId,
            0,
            0);
    }
}