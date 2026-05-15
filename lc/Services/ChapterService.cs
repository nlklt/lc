using lc.Data.Repositories.Interfaces;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
using lc.Services.Interfaces;

namespace lc.Services;

public sealed class ChapterService : IChapterService
{
    private readonly IBookRepository _bookRepository;
    private readonly IChapterRepository _chapterRepository;
    private readonly IBookStatsService _bookStatsService;

    public ChapterService(
        IBookRepository bookRepository,
        IChapterRepository chapterRepository,
        IBookStatsService bookStatsService)
    {
        _bookRepository = bookRepository ?? throw new ArgumentNullException(nameof(bookRepository));
        _chapterRepository = chapterRepository ?? throw new ArgumentNullException(nameof(chapterRepository));
        _bookStatsService = bookStatsService ?? throw new ArgumentNullException(nameof(bookStatsService));
    }

    public Task<Chapter?> GetByIdAsync(int chapterId)
        => _chapterRepository.GetByIdAsync(chapterId);

    public Task<IReadOnlyList<Chapter>> GetByBookIdAsync(int bookId)
        => _chapterRepository.GetByBookIdAsync(bookId);

    public async Task<int> SaveAsync(int bookId, Chapter chapter)
    {
        ArgumentNullException.ThrowIfNull(chapter);

        var book = await _bookRepository.GetByIdAsync(bookId, includeChapters: true)
            ?? throw new InvalidOperationException($"Книга с BookId={bookId} не найдена.");

        if (book.BookStatus != Models.Enums.BookStatus.Draft)
            throw new InvalidOperationException("Главы можно менять только у черновика.");

        chapter.BookId = bookId;

        if (chapter.ChapterNumber <= 0)
            chapter.ChapterNumber = (book.Chapters?.Count ?? 0) + 1;

        int chapterId;

        if (chapter.ChapterId == 0)
        {
            chapterId = await _chapterRepository.CreateAsync(chapter);
        }
        else
        {
            await _chapterRepository.UpdateAsync(chapter);
            chapterId = chapter.ChapterId;
        }

        await _bookStatsService.RecalculateBookCacheAsync(bookId);
        return chapterId;
    }

    public async Task DeleteAsync(int chapterId)
    {
        var chapter = await _chapterRepository.GetByIdAsync(chapterId);
        if (chapter is null)
            return;

        var book = await _bookRepository.GetByIdAsync(chapter.BookId);
        if (book is null)
            return;

        if (book.BookStatus != Models.Enums.BookStatus.Draft)
            throw new InvalidOperationException("Главы можно удалять только у черновика.");

        await _chapterRepository.DeleteAsync(chapterId);
        await _bookStatsService.RecalculateBookCacheAsync(chapter.BookId);
    }
}