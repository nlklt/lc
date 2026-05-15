using lc.Data.Repositories.Interfaces;
using lc.Helpers;
using lc.Infrastructure;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
using lc.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace lc.Infrastructure.Repositories.Sql;

public sealed class BookRepository : IBookRepository
{
    private readonly AppDbContext _db;

    public BookRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Book?> GetByIdAsync(int bookId, bool includeChapters = false, bool includeComments = false)
    {
        IQueryable<Book> query = _db.Books
            .AsNoTracking()
            .Include(b => b.Publisher)
            .Include(b => b.Tags)
            .Include(b => b.Categories);

        if (includeChapters)
            query = query.Include(b => b.Chapters);

        if (includeComments)
            query = query.Include(b => b.Comments);

        return await query
            .AsSplitQuery()
            .FirstOrDefaultAsync(b => b.BookId == bookId);
    }

    public async Task<int> CreateAsync(Book book)
    {
        ArgumentNullException.ThrowIfNull(book);

        var tagIds = book.Tags.Select(t => t.TagId).Distinct().ToArray();
        var categoryIds = book.Categories.Select(c => c.CategoryId).Distinct().ToArray();

        var entity = new Book
        {
            Title = string.IsNullOrWhiteSpace(book.Title) ? "Без названия" : book.Title,
            PublisherId = book.PublisherId,
            AuthorName = book.AuthorName,
            Description = book.Description,
            CoverImagePath = book.CoverImagePath,
            BookStatus = book.BookStatus,
            WritingStatus = book.WritingStatus,
            Language = book.Language,
            AgeRating = book.AgeRating,
            SymbolsCount = book.SymbolsCount,
            ChaptersCount = book.ChaptersCount,
            Views = book.Views,
            Rating = book.Rating,
            CreatedAt = book.CreatedAt == default ? DateTime.Now : book.CreatedAt,
            UpdatedAt = book.UpdatedAt == default ? DateTime.Now : book.UpdatedAt
        };

        if (tagIds.Length > 0)
        {
            entity.Tags = await _db.Tags
                .Where(t => tagIds.Contains(t.TagId))
                .ToListAsync();
        }

        if (categoryIds.Length > 0)
        {
            entity.Categories = await _db.Categories
                .Where(c => categoryIds.Contains(c.CategoryId))
                .ToListAsync();
        }

        _db.Books.Add(entity);
        await _db.SaveChangesAsync();

        return entity.BookId;
    }

    public async Task UpdateAsync(Book book)
    {
        ArgumentNullException.ThrowIfNull(book);

        var existing = await _db.Books
            .Include(b => b.Tags)
            .Include(b => b.Categories)
            .FirstOrDefaultAsync(b => b.BookId == book.BookId)
            ?? throw new InvalidOperationException($"Книга с BookId={book.BookId} не найдена.");

        var createdAt = existing.CreatedAt;

        _db.Entry(existing).CurrentValues.SetValues(book);
        existing.CreatedAt = createdAt;
        existing.UpdatedAt = DateTime.Now;
        existing.Title = string.IsNullOrWhiteSpace(book.Title) ? "Без названия" : book.Title;

        var tagIds = book.Tags.Select(t => t.TagId).Distinct().ToArray();
        var categoryIds = book.Categories.Select(c => c.CategoryId).Distinct().ToArray();

        existing.Tags.Clear();
        if (tagIds.Length > 0)
        {
            var tags = await _db.Tags
                .Where(t => tagIds.Contains(t.TagId))
                .ToListAsync();

            foreach (var tag in tags)
                existing.Tags.Add(tag);
        }

        existing.Categories.Clear();
        if (categoryIds.Length > 0)
        {
            var categories = await _db.Categories
                .Where(c => categoryIds.Contains(c.CategoryId))
                .ToListAsync();

            foreach (var category in categories)
                existing.Categories.Add(category);
        }

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int bookId)
    {
        var book = await _db.Books
            .FirstOrDefaultAsync(b => b.BookId == bookId);

        if (book is null)
            return;

        _db.Books.Remove(book);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateStatusAsync(int bookId, BookStatus status)
    {
        var book = await _db.Books
            .FirstOrDefaultAsync(b => b.BookId == bookId);

        if (book is null)
            return;

        book.BookStatus = status;
        book.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<BookListItemDto>> SearchAsync(BookFilterCriteria criteria)
    {
        IQueryable<Book> query = _db.Books.AsNoTracking();

        query = ApplyFilters(query, criteria);

        IQueryable<BookListItemDto> projected = query.Select(b => new BookListItemDto
        {
            BookId = b.BookId,
            PublisherId = b.PublisherId,
            Title = b.Title,
            AuthorName = b.AuthorName,
            CoverImagePath = b.CoverImagePath,
            BookStatus = b.BookStatus,
            WritingStatus = b.WritingStatus,
            Language = b.Language,
            AgeRating = b.AgeRating,
            Views = b.Views,
            Rating = b.Rating,
            ChaptersCount = b.Chapters.Count(),
            SymbolsCount = b.Chapters.Sum(c => (long?)c.Text.Length) ?? 0,
            CreatedAt = b.CreatedAt,
            UpdatedAt = b.UpdatedAt
        });

        projected = ApplyOrdering(projected, criteria);

        var items = await projected.ToListAsync();

        if (items.Count == 0)
            return items;

        var bookIds = items.Select(x => x.BookId).ToArray();

        var tagsByBook = await _db.BookTags
            .AsNoTracking()
            .Where(bt => bookIds.Contains(bt.BookId))
            .Join(_db.Tags.AsNoTracking(),
                bt => bt.TagId,
                t => t.TagId,
                (bt, t) => new
                {
                    bt.BookId,
                    Tag = new Tag { TagId = t.TagId, Name = t.Name }
                })
            .GroupBy(x => x.BookId)
            .ToDictionaryAsync(
                g => g.Key,
                g => (IReadOnlyList<Tag>)g
                    .Select(x => x.Tag)
                    .OrderBy(t => t.Name)
                    .ToList());

        var categoriesByBook = await _db.BookCategories
            .AsNoTracking()
            .Where(bc => bookIds.Contains(bc.BookId))
            .Join(_db.Categories.AsNoTracking(),
                bc => bc.CategoryId,
                c => c.CategoryId,
                (bc, c) => new
                {
                    bc.BookId,
                    Category = new Category { CategoryId = c.CategoryId, Name = c.Name }
                })
            .GroupBy(x => x.BookId)
            .ToDictionaryAsync(
                g => g.Key,
                g => (IReadOnlyList<Category>)g
                    .Select(x => x.Category)
                    .OrderBy(c => c.Name)
                    .ToList());

        foreach (var item in items)
        {
            item.Tags = tagsByBook.TryGetValue(item.BookId, out var tags) ? tags.ToList() : [];
            item.Categories = categoriesByBook.TryGetValue(item.BookId, out var categories) ? categories.ToList() : [];
        }

        return items;
    }

    private static IQueryable<Book> ApplyFilters(IQueryable<Book> query, BookFilterCriteria criteria)
    {
        if (!string.IsNullOrWhiteSpace(criteria.SearchText))
        {
            var text = criteria.SearchText.Trim();
            query = query.Where(b =>
                b.Title.Contains(text) ||
                (b.AuthorName != null && b.AuthorName.Contains(text)));
        }

        if (criteria.IncludeBookStatuses.Count > 0)
            query = query.Where(b => criteria.IncludeBookStatuses.Contains(b.BookStatus));

        if (criteria.ExcludeBookStatuses.Count > 0)
            query = query.Where(b => !criteria.ExcludeBookStatuses.Contains(b.BookStatus));

        if (criteria.IncludeWritingStatuses.Count > 0)
            query = query.Where(b => criteria.IncludeWritingStatuses.Contains(b.WritingStatus));

        if (criteria.ExcludeWritingStatuses.Count > 0)
            query = query.Where(b => !criteria.ExcludeWritingStatuses.Contains(b.WritingStatus));

        if (criteria.IncludeLanguages.Count > 0)
            query = query.Where(b => criteria.IncludeLanguages.Contains(b.Language));

        if (criteria.ExcludeLanguages.Count > 0)
            query = query.Where(b => !criteria.ExcludeLanguages.Contains(b.Language));

        if (criteria.IncludeAgeRatings.Count > 0)
            query = query.Where(b => criteria.IncludeAgeRatings.Contains(b.AgeRating));

        if (criteria.ExcludeAgeRatings.Count > 0)
            query = query.Where(b => !criteria.ExcludeAgeRatings.Contains(b.AgeRating));

        if (criteria.RatingFrom.HasValue)
            query = query.Where(b => b.Rating >= criteria.RatingFrom.Value);

        if (criteria.RatingTo.HasValue)
            query = query.Where(b => b.Rating <= criteria.RatingTo.Value);

        if (criteria.CreatedFrom.HasValue)
            query = query.Where(b => b.CreatedAt >= criteria.CreatedFrom.Value);

        if (criteria.CreatedTo.HasValue)
            query = query.Where(b => b.CreatedAt <= criteria.CreatedTo.Value);

        if (criteria.ChaptersFrom.HasValue)
            query = query.Where(b => b.ChaptersCount >= criteria.ChaptersFrom.Value);

        if (criteria.ChaptersTo.HasValue)
            query = query.Where(b => b.ChaptersCount <= criteria.ChaptersTo.Value);

        if (criteria.SymbolsFrom.HasValue)
            query = query.Where(b => b.SymbolsCount >= criteria.SymbolsFrom.Value);

        if (criteria.SymbolsTo.HasValue)
            query = query.Where(b => b.SymbolsCount <= criteria.SymbolsTo.Value);

        if (criteria.ExcludeTagIds.Count > 0)
            query = query.Where(b => !b.Tags.Any(t => criteria.ExcludeTagIds.Contains(t.TagId)));

        if (criteria.IncludeTagIds.Count > 0)
        {
            query = criteria.StrictTagMatch
                ? query.Where(b => criteria.IncludeTagIds.All(tagId => b.Tags.Any(t => t.TagId == tagId)))
                : query.Where(b => b.Tags.Any(t => criteria.IncludeTagIds.Contains(t.TagId)));
        }

        if (criteria.ExcludeCategoryIds.Count > 0)
            query = query.Where(b => !b.Categories.Any(c => criteria.ExcludeCategoryIds.Contains(c.CategoryId)));

        if (criteria.IncludeCategoryIds.Count > 0)
        {
            query = criteria.StrictCategoryMatch
                ? query.Where(b => criteria.IncludeCategoryIds.All(categoryId => b.Categories.Any(c => c.CategoryId == categoryId)))
                : query.Where(b => b.Categories.Any(c => criteria.IncludeCategoryIds.Contains(c.CategoryId)));
        }

        return query;
    }

    private static IQueryable<BookListItemDto> ApplyOrdering(IQueryable<BookListItemDto> query, BookFilterCriteria criteria)
    {
        return criteria.SortField switch
        {
            nameof(BookListItemDto.Title) => criteria.SortAscending
                ? query.OrderBy(x => x.Title)
                : query.OrderByDescending(x => x.Title),

            nameof(BookListItemDto.AuthorName) => criteria.SortAscending
                ? query.OrderBy(x => x.AuthorName)
                : query.OrderByDescending(x => x.AuthorName),

            nameof(BookListItemDto.Rating) => criteria.SortAscending
                ? query.OrderBy(x => x.Rating)
                : query.OrderByDescending(x => x.Rating),

            nameof(BookListItemDto.Views) => criteria.SortAscending
                ? query.OrderBy(x => x.Views)
                : query.OrderByDescending(x => x.Views),

            nameof(BookListItemDto.ChaptersCount) => criteria.SortAscending
                ? query.OrderBy(x => x.ChaptersCount)
                : query.OrderByDescending(x => x.ChaptersCount),

            nameof(BookListItemDto.SymbolsCount) => criteria.SortAscending
                ? query.OrderBy(x => x.SymbolsCount)
                : query.OrderByDescending(x => x.SymbolsCount),

            nameof(BookListItemDto.BookStatus) => criteria.SortAscending
                ? query.OrderBy(x => x.BookStatus)
                : query.OrderByDescending(x => x.BookStatus),

            nameof(BookListItemDto.WritingStatus) => criteria.SortAscending
                ? query.OrderBy(x => x.WritingStatus)
                : query.OrderByDescending(x => x.WritingStatus),

            nameof(BookListItemDto.CreatedAt) => criteria.SortAscending
                ? query.OrderBy(x => x.CreatedAt)
                : query.OrderByDescending(x => x.CreatedAt),

            _ => criteria.SortAscending
                ? query.OrderBy(x => x.Title)
                : query.OrderByDescending(x => x.Title)
        };
    }
}