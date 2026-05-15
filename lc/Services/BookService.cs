using lc.Data.Repositories.Interfaces;
using lc.Helpers;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
using lc.Models.Enums;
using lc.Services.Interfaces;

namespace lc.Services;

public sealed class BookService : IBookService
{
    private readonly IBookRepository _bookRepository;
    private readonly ITagRepository _tagRepository;
    private readonly ICategoryRepository _categoryRepository;

    public BookService(
        IBookRepository bookRepository,
        ITagRepository tagRepository,
        ICategoryRepository categoryRepository)
    {
        _bookRepository = bookRepository ?? throw new ArgumentNullException(nameof(bookRepository));
        _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
    }

    public Task<Book?> GetBookByIdAsync(int bookId)
        => _bookRepository.GetByIdAsync(bookId);

    public Task<IReadOnlyList<BookListItemDto>> GetCatalogAsync(BookFilterCriteria criteria)
        => _bookRepository.SearchAsync(criteria);

    public Task<IReadOnlyList<Category>> GetAllCategoriesAsync()
        => _categoryRepository.GetAllAsync();

    public Task<IReadOnlyList<Tag>> GetAllTagsAsync()
        => _tagRepository.GetAllAsync();

    public async Task<int> CreateBookAsync(Book book)
    {
        ArgumentNullException.ThrowIfNull(book);

        NormalizeDraft(book);

        book.BookStatus = BookStatus.Draft;
        book.CreatedAt = DateTime.Now;
        book.UpdatedAt = DateTime.Now;

        return await _bookRepository.CreateAsync(book);
    }

    public async Task UpdateBookAsync(Book book)
    {
        ArgumentNullException.ThrowIfNull(book);

        var existing = await _bookRepository.GetByIdAsync(book.BookId)
            ?? throw new InvalidOperationException($"Книга с BookId={book.BookId} не найдена.");

        NormalizeDraft(book);

        book.CreatedAt = existing.CreatedAt;
        book.UpdatedAt = DateTime.Now;
        book.BookStatus = BookStatus.Draft;

        await _bookRepository.UpdateAsync(book);
    }

    public async Task ArchiveBookAsync(int bookId)
    {
        var book = await _bookRepository.GetByIdAsync(bookId);
        if (book is null)
            return;

        if (book.BookStatus == BookStatus.Archived)
            return;

        await _bookRepository.UpdateStatusAsync(bookId, BookStatus.Archived);
    }

    public async Task RestoreBookAsync(int bookId)
    {
        var book = await _bookRepository.GetByIdAsync(bookId)
            ?? throw new InvalidOperationException($"Книга с BookId={bookId} не найдена.");

        if (book.BookStatus != BookStatus.Archived)
            return;

        await _bookRepository.UpdateStatusAsync(bookId, BookStatus.Published);
    }

    public async Task DeleteBookAsync(int bookId)
    {
        await _bookRepository.DeleteAsync(bookId);
    }

    public async Task PublishBookAsync(int bookId)
    {
        var book = await _bookRepository.GetByIdAsync(bookId, includeChapters: true)
            ?? throw new InvalidOperationException($"Книга с BookId={bookId} не найдена.");

        if (book.BookStatus == BookStatus.Published)
            return;

        var chapters = (book.Chapters ?? [])
            .OrderBy(c => c.ChapterNumber)
            .ToList();

        ValidateForPublish(book, chapters);

        book.BookStatus = BookStatus.Published;
        book.UpdatedAt = DateTime.Now;

        await _bookRepository.UpdateAsync(book);
    }

    private static void NormalizeDraft(Book book)
    {
        book.Title = string.IsNullOrWhiteSpace(book.Title) ? "Без названия" : book.Title.Trim();
        book.AuthorName = NormalizeString(book.AuthorName);
        book.Description = NormalizeString(book.Description);
        book.CoverImagePath = NormalizeString(book.CoverImagePath);

        if (book.Views < 0)
            book.Views = 0;

        if (book.Rating < 0)
            book.Rating = 0;
    }

    private static string? NormalizeString(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ValidateForPublish(Book book, IReadOnlyList<Chapter> chapters)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(book.Title))
            errors.Add("Название книги");

        if (string.IsNullOrWhiteSpace(book.AuthorName))
            errors.Add("Автор");

        if (string.IsNullOrWhiteSpace(book.Description))
            errors.Add("Описание");

        if (string.IsNullOrWhiteSpace(book.CoverImagePath))
            errors.Add("Обложка");

        if (book.Language == default)
            errors.Add("Язык");

        if (book.AgeRating <= 0)
            errors.Add("Возрастной рейтинг");

        if (chapters.Count == 0)
            errors.Add("Хотя бы одна глава");

        foreach (var chapter in chapters)
        {
            if (string.IsNullOrWhiteSpace(chapter.Title))
                errors.Add($"Заголовок главы №{chapter.ChapterNumber}");

            if (string.IsNullOrWhiteSpace(chapter.Text))
                errors.Add($"Текст главы №{chapter.ChapterNumber}");
        }

        if (errors.Count > 0)
            throw new InvalidOperationException(
                "Нельзя опубликовать книгу. Не заполнены поля: " + string.Join(", ", errors));
    }
}