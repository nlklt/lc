using lc.Data.Repositories.Interfaces;
using lc.Infrastructure;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
using lc.Models.Enums;
using lc.Services.Interfaces;

namespace lc.Services;

public sealed class ChapterService : IChapterService
{
    private readonly AppState _appState;
    private readonly IBookRepository _bookRepository;
    private readonly IChapterRepository _chapterRepository;
    private readonly IBookStatsService _bookStatsService;

    public ChapterService(
        IBookRepository bookRepository,
        IChapterRepository chapterRepository,
        IBookStatsService bookStatsService,
        AppState appState)
    {
        _bookRepository = bookRepository ?? throw new ArgumentNullException(nameof(bookRepository));
        _chapterRepository = chapterRepository ?? throw new ArgumentNullException(nameof(chapterRepository));
        _bookStatsService = bookStatsService ?? throw new ArgumentNullException(nameof(bookStatsService));
        _appState = appState ?? throw new ArgumentNullException(nameof(appState));
    }

    public Task<Chapter?> GetByIdAsync(int chapterId)
        => _chapterRepository.GetByIdAsync(chapterId);

    public Task<IReadOnlyList<Chapter>> GetByBookIdAsync(int bookId, bool includeDrafts)
        => _chapterRepository.GetByBookIdAsync(bookId, includeDrafts);

    public async Task<int> SaveAsync(int bookId, Chapter chapter, ChapterStatus targetStatus)
    {
        ArgumentNullException.ThrowIfNull(chapter);

        var book = await _bookRepository.GetByIdAsync(bookId, includeChapters: true)
            ?? throw new InvalidOperationException($"Книга с BookId={bookId} не найдена.");

        if (_appState.CurrentUser is null)
            throw new InvalidOperationException("Пользователь не авторизован.");

        if (chapter.ChapterId == 0)
        {
            EnsureCanAddChapter(book);

            chapter.BookId = bookId;
            chapter.ChapterNumber = await GetNextChapterNumberAsync(bookId);
            chapter.Status = targetStatus;

            NormalizeChapter(chapter);

            var chapterId = await _chapterRepository.CreateAsync(chapter);
            await _bookStatsService.RecalculateBookCacheAsync(bookId);
            return chapterId;
        }

        var existing = await _chapterRepository.GetByIdAsync(chapter.ChapterId)
            ?? throw new InvalidOperationException($"Глава с ChapterId={chapter.ChapterId} не найдена.");

        if (existing.BookId != bookId)
            throw new InvalidOperationException("Нельзя менять книгу у главы.");

        EnsureCanEditChapter(book, existing);

        chapter.BookId = bookId;
        chapter.ChapterId = existing.ChapterId;
        chapter.ChapterNumber = existing.ChapterNumber;
        chapter.Status = existing.Status;

        NormalizeChapter(chapter);

        await _chapterRepository.UpdateAsync(chapter);
        await _bookStatsService.RecalculateBookCacheAsync(bookId);
        return chapter.ChapterId;
    }

    public async Task DeleteAsync(int chapterId)
    {
        var chapter = await _chapterRepository.GetByIdAsync(chapterId)
            ?? throw new InvalidOperationException($"Глава с ChapterId={chapterId} не найдена.");

        var book = await _bookRepository.GetByIdAsync(chapter.BookId, includeChapters: true)
            ?? throw new InvalidOperationException($"Книга с BookId={chapter.BookId} не найдена.");

        EnsureCanDeleteChapter(book, chapter);

        await _chapterRepository.DeleteAsync(chapterId);
        await RenumberChaptersAsync(book.BookId);
        await _bookStatsService.RecalculateBookCacheAsync(book.BookId);
    }

    private async Task<int> GetNextChapterNumberAsync(int bookId)
    {
        var chapters = await _chapterRepository.GetByBookIdAsync(bookId, includeDrafts: true);
        return chapters.Count == 0 ? 1 : chapters.Max(x => x.ChapterNumber) + 1;
    }

    private async Task RenumberChaptersAsync(int bookId)
    {
        var chapters = (await _chapterRepository.GetByBookIdAsync(bookId, includeDrafts: true))
            .OrderBy(x => x.ChapterNumber)
            .ToList();

        for (var i = 0; i < chapters.Count; i++)
        {
            var chapter = chapters[i];
            var newNumber = i + 1;

            if (chapter.ChapterNumber == newNumber)
                continue;

            chapter.ChapterNumber = newNumber;
            await _chapterRepository.UpdateAsync(chapter);
        }
    }

    private void ValidatePermissions(Book book, Chapter chapter)
    {
        if (_appState.CurrentUser is null)
            throw new InvalidOperationException("Пользователь не авторизован.");

        if (_appState.IsAdmin)
            return;

        if (book.BookStatus == BookStatus.Archived)
            throw new InvalidOperationException("Редактирование книги в архиве доступно только администратору.");

        if (_appState.CurrentUser.UserId != book.PublisherId)
            throw new InvalidOperationException("Недостаточно прав для редактирования глав этой книги.");
    }

    private void EnsureCanAddChapter(Book book)
    {
        if (_appState.CurrentUser is null)
            throw new InvalidOperationException("Пользователь не авторизован.");

        if (book.BookStatus == BookStatus.Archived)
            throw new InvalidOperationException("В архивированную книгу нельзя добавлять новые главы.");

        if (_appState.CurrentUser.UserId != book.PublisherId && !_appState.IsAdmin)
            throw new InvalidOperationException("Недостаточно прав для добавления главы.");
    }

    private void EnsureCanEditChapter(Book book, Chapter existing)
    {
        if (_appState.CurrentUser is null)
            throw new InvalidOperationException("Пользователь не авторизован.");

        if (book.BookStatus == BookStatus.Archived)
        {
            if (!_appState.IsAdmin)
                throw new InvalidOperationException("Редактирование глав в архиве доступно только администратору.");

            return;
        }

        if (existing.Status != ChapterStatus.Draft)
            throw new InvalidOperationException("Редактировать можно только главу в статусе Draft.");

        if (_appState.CurrentUser.UserId != book.PublisherId && !_appState.IsAdmin)
            throw new InvalidOperationException("Недостаточно прав для редактирования главы.");
    }

    private void EnsureCanDeleteChapter(Book book, Chapter chapter)
    {
        if (_appState.CurrentUser is null)
            throw new InvalidOperationException("Пользователь не авторизован.");

        if (book.BookStatus == BookStatus.Archived)
        {
            if (!_appState.IsAdmin)
                throw new InvalidOperationException("Удаление глав в архиве доступно только администратору.");

            return;
        }

        if (chapter.Status != ChapterStatus.Draft)
            throw new InvalidOperationException("Удалять можно только главу в статусе Draft.");

        if (_appState.CurrentUser.UserId != book.PublisherId && !_appState.IsAdmin)
            throw new InvalidOperationException("Недостаточно прав для удаления главы.");
    }

    private static void NormalizeChapter(Chapter chapter)
    {
        chapter.Title = string.IsNullOrWhiteSpace(chapter.Title) ? "Без названия" : chapter.Title.Trim();
        chapter.Text = string.IsNullOrWhiteSpace(chapter.Text) ? string.Empty : chapter.Text.Trim();
    }
}