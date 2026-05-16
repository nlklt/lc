using lc.Data.Repositories.Interfaces;
using lc.Helpers;
using lc.Infrastructure;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
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
        var items = await _db.UserLibraryListBooks
            .AsNoTracking()
            .Where(x => x.ListId == listId && x.List.UserId == userId)
            .OrderByDescending(x => x.AddedAt)
            .Select(x => new BookListItemDto
            {
                BookId = x.Book.BookId,
                PublisherId = x.Book.PublisherId,
                Title = x.Book.Title,
                AuthorName = x.Book.AuthorName,
                Description = x.Book.Description,
                CoverImagePath = x.Book.CoverImagePath,
                BookStatus = x.Book.BookStatus,
                WritingStatus = x.Book.WritingStatus,
                Language = x.Book.Language,
                AgeRating = x.Book.AgeRating,
                SymbolsCount = x.Book.Chapters.Sum(c => (long?)c.Text.Length) ?? 0,
                ChaptersCount = x.Book.Chapters.Count,
                Views = x.Book.Views,
                Rating = x.Book.Rating,
                CreatedAt = x.Book.CreatedAt,
                UpdatedAt = x.Book.UpdatedAt
            })
            .ToListAsync();

        await PopulateRelationsAsync(items);
        await PopulateReadingProgressAsync(userId, items);

        return items;
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

    public async Task AddBookAsync(int userId, int listId, int bookId)
    {
        var list = await _db.UserLibraryLists
            .FirstOrDefaultAsync(x => x.ListId == listId && x.UserId == userId);

        if (list is null)
            throw new InvalidOperationException($"Список с ListId={listId} не найден.");

        var bookExists = await _db.Books.AnyAsync(x => x.BookId == bookId);
        if (!bookExists)
            throw new InvalidOperationException($"Книга с BookId={bookId} не найдена.");

        var exists = await _db.UserLibraryListBooks.AnyAsync(x =>
            x.ListId == listId &&
            x.BookId == bookId);

        if (exists)
            return;

        _db.UserLibraryListBooks.Add(new UserLibraryListBook
        {
            ListId = listId,
            BookId = bookId,
            AddedAt = DateTime.Now
        });

        await _db.SaveChangesAsync();
    }

    public async Task RemoveBookAsync(int userId, int listId, int bookId)
    {
        var item = await _db.UserLibraryListBooks
            .FirstOrDefaultAsync(x =>
                x.ListId == listId &&
                x.List.UserId == userId &&
                x.BookId == bookId);

        if (item is null)
            return;

        _db.UserLibraryListBooks.Remove(item);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(int userId, int listId, int bookId)
    {
        return await _db.UserLibraryListBooks.AnyAsync(x =>
            x.ListId == listId &&
            x.List.UserId == userId &&
            x.BookId == bookId);
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