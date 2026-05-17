using lc.Data.Repositories.Interfaces;
using lc.Helpers;
using lc.Infrastructure;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
using lc.Models.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace lc.Infrastructure.Repositories.Sql;

public sealed class UserLibraryListBookRepository : IUserLibraryListBookRepository
{
    private readonly AppDbContext _db;

    public UserLibraryListBookRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<BookListItemDto>> GetBooksAsync(int userId, int listId)
    {
        return await _db.UserLibraryListBooks
            .AsNoTracking()
            .Where(x => x.ListId == listId && x.List.UserId == userId)
            .Select(x => new BookListItemDto
            {
                BookId = x.BookId,
                Title = x.Book.Title,
                CoverImagePath = x.Book.CoverImagePath
            })
            .ToListAsync();
    }

    private async Task PopulateReadingProgressAsync(int userId, List<BookListItemDto> items)
    {
        if (items.Count == 0)
            return;

        var bookIds = items.Select(x => x.BookId).ToArray();
        
        var progressByBook = await _db.ReadingProgresses
            .AsNoTracking()
            .Where(x => x.UserId == userId && bookIds.Contains(x.BookId))
            .GroupBy(x => x.BookId)
            .Select(g => new
            {
                BookId = g.Key,
                Percent = g.Max(x => x.ProgressPercent)
            })
            .ToDictionaryAsync(x => x.BookId, x => x.Percent);

        foreach (var item in items)
        {
            item.ReadingProgressPercent = progressByBook.TryGetValue(item.BookId, out var percent)
                ? percent
                : 0;
        }
    }

    public async Task<bool> AddBookAsync(int userId, int listId, int bookId)
    {
        if (userId <= 0) throw new ArgumentOutOfRangeException(nameof(userId));
        if (listId <= 0) throw new ArgumentOutOfRangeException(nameof(listId));
        if (bookId <= 0) throw new ArgumentOutOfRangeException(nameof(bookId));

        var listExists = await _db.UserLibraryLists
            .AsNoTracking()
            .AnyAsync(x => x.UserId == userId && x.ListId == listId);

        if (!listExists)
            throw new InvalidOperationException("Список не найден или не принадлежит пользователю.");

        var book = await _db.Books
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.BookId == bookId);

        if (book is null)
            throw new InvalidOperationException("Книга не найдена.");

        if (book.BookStatus != BookStatus.Published)
            throw new InvalidOperationException("В личные списки можно добавлять только опубликованные книги.");

        var entity = new UserLibraryListBook
        {
            ListId = listId,
            BookId = bookId
        };

        _db.UserLibraryListBooks.Add(entity);

        try
        {
            await _db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return false;
        }
    }

    public async Task RemoveBookAsync(int userId, int listId, int bookId)
    {
        var entity = await _db.UserLibraryListBooks
            .FirstOrDefaultAsync(x => x.ListId == listId &&
                                      x.BookId == bookId &&
                                      x.List.UserId == userId);

        if (entity is null)
            return;

        _db.UserLibraryListBooks.Remove(entity);
        await _db.SaveChangesAsync();
    }

    private static bool IsUniqueViolation(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is SqlException sql && (sql.Number == 2601 || sql.Number == 2627))
                return true;
        }

        return false;
    }

    public Task<bool> ExistsAsync(int userId, int listId, int bookId)
    {
        return _db.UserLibraryListBooks
            .AsNoTracking()
            .AnyAsync(x => x.ListId == listId &&
                           x.BookId == bookId &&
                           x.List.UserId == userId);
    }

    public Task<bool> ExistsInAnyListAsync(int userId, int bookId)
    {
        return _db.UserLibraryListBooks
            .AsNoTracking()
            .AnyAsync(x => x.BookId == bookId &&
                           x.List.UserId == userId);
    }

    private async Task PopulateRelationsAsync(List<BookListItemDto> items)
    {
        if (items.Count == 0)
            return;

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
            item.Tags = tagsByBook.TryGetValue(item.BookId, out var tags)
                ? tags.ToList()
                : [];

            item.Categories = categoriesByBook.TryGetValue(item.BookId, out var categories)
                ? categories.ToList()
                : [];
        }
    }
}