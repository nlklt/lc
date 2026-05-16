using lc.Data.Repositories.Interfaces;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
using lc.Models.Enums;
using lc.Services.Interfaces;

namespace lc.Services;

public sealed class ReaderService : IReaderService
{
    private readonly IBookRepository _bookRepository;

    public ReaderService(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository ?? throw new ArgumentNullException(nameof(bookRepository));
    }

    public async Task<ReaderSession?> OpenAsync(int bookId, int? chapterNumber = null)
    {
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

        var currentChapter = chapterNumber.HasValue
            ? chapters.FirstOrDefault(c => c.ChapterNumber == chapterNumber.Value) ?? chapters[0]
            : chapters[0];

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