using lc.Data.Repositories.Interfaces;
using lc.Helpers;
using lc.Infrastructure;
using lc.Models;
using lc.Models.Enums;
using lc.Services.Interfaces;

namespace lc.Services;

public sealed class ReaderService : IReaderService
{
    private readonly IBookRepository _bookRepository;
    private readonly AppState _appState;
    private readonly IReadingProgressService _readingProgressService;

    public ReaderService(
        IBookRepository bookRepository,
        AppState appState,
        IReadingProgressService readingProgressService)
    {
        _bookRepository = bookRepository ?? throw new ArgumentNullException(nameof(bookRepository));
        _appState = appState ?? throw new ArgumentNullException(nameof(appState));
        _readingProgressService = readingProgressService ?? throw new ArgumentNullException(nameof(readingProgressService));
    }

    public async Task<ReaderSession?> OpenAsync(int bookId, int? chapterNumber = null)
    {
        if (bookId <= 0)
            throw new ArgumentOutOfRangeException(nameof(bookId));

        var book = await _bookRepository.GetByIdAsync(bookId, includeChapters: true);
        if (book is null)
            return null;

        if (book.BookStatus != BookStatus.Published)
            throw new InvalidOperationException("В ридер можно открыть только опубликованную книгу.");

        var chapters = (book.Chapters ?? [])
            .Where(c => c.Status == ChapterStatus.Published)
            .OrderBy(c => c.ChapterNumber)
            .ToList();

        if (chapters.Count == 0)
            throw new InvalidOperationException("У книги нет опубликованных глав.");

        Chapter? currentChapter = null;

        if (chapterNumber.HasValue)
        {
            currentChapter = chapters.FirstOrDefault(c => c.ChapterNumber == chapterNumber.Value);
        }
        else if (_appState.CurrentUser is not null)
        {
            var progress = await _readingProgressService.GetLastBookProgressAsync(_appState.CurrentUser.UserId, bookId);
            if (progress is not null)
                currentChapter = chapters.FirstOrDefault(c => c.ChapterId == progress.ChapterId);
        }

        currentChapter ??= chapters[0];

        var index = chapters.FindIndex(c => c.ChapterId == currentChapter.ChapterId);

        return new ReaderSession
        {
            Book = book,
            Chapters = chapters,
            CurrentChapter = currentChapter,
            PreviousChapter = index > 0 ? chapters[index - 1] : null,
            NextChapter = index >= 0 && index < chapters.Count - 1 ? chapters[index + 1] : null
        };
    }
}