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
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public UserLibraryListBookRepository(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IReadOnlyList<BookListItemDto>> GetBooksAsync(int userId, int listId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.UserLibraryListBooks
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

    public async Task<bool> AddBookAsync(int userId, int listId, int bookId)
    {
        if (userId <= 0) throw new ArgumentOutOfRangeException(nameof(userId));
        if (listId <= 0) throw new ArgumentOutOfRangeException(nameof(listId));
        if (bookId <= 0) throw new ArgumentOutOfRangeException(nameof(bookId));

        await using var db = await _dbFactory.CreateDbContextAsync();

        var listExists = await db.UserLibraryLists
            .AsNoTracking()
            .AnyAsync(x => x.UserId == userId && x.ListId == listId);

        if (!listExists)
            throw new InvalidOperationException("Список не найден или не принадлежит пользователю.");

        var book = await db.Books
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

        db.UserLibraryListBooks.Add(entity);

        try
        {
            await db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return false;
        }
    }

    public async Task RemoveBookAsync(int userId, int listId, int bookId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var entity = await db.UserLibraryListBooks
            .FirstOrDefaultAsync(x => x.ListId == listId &&
                                      x.BookId == bookId &&
                                      x.List.UserId == userId);

        if (entity is null)
            return;

        db.UserLibraryListBooks.Remove(entity);
        await db.SaveChangesAsync();
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

    public async Task<bool> ExistsAsync(int userId, int listId, int bookId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.UserLibraryListBooks
            .AsNoTracking()
            .AnyAsync(x => x.ListId == listId &&
                           x.BookId == bookId &&
                           x.List.UserId == userId);
    }

    public async Task<bool> ExistsInAnyListAsync(int userId, int bookId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.UserLibraryListBooks
            .AsNoTracking()
            .AnyAsync(x => x.BookId == bookId &&
                           x.List.UserId == userId);
    }
}