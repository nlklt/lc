using lc.Data;
using lc.Data.Repositories.Interfaces;
using lc.Infrastructure.Data;
using lc.Models;
using lc.Models.Enums;
using lc.Services.Interfaces;
using Microsoft.Data.SqlClient;

namespace lc.Infrastructure.Repositories.Sql
{
    public sealed class UserLibraryRepository : IUserLibraryRepository
    {
        public async Task<IReadOnlyList<UserLibraryListDto>> GetListsAsync(int userId)
        {
            const string sql = @"
            SELECT ListId, Name
            FROM UserLibraryLists
            WHERE UserId = @UserId
            ORDER BY Name;";

            await using var connection = SqlConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            var result = new List<UserLibraryListDto>();
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new UserLibraryListDto
                {
                    ListId = reader.GetInt32(reader.GetOrdinal("ListId")),
                    Name = reader.GetString(reader.GetOrdinal("Name"))
                });
            }

            return result;
        }

        public async Task<IReadOnlyList<BookListItem>> GetBooksFromListAsync(int userId, int listId)
        {
            const string sql = @"
            SELECT
                b.BookId,
                b.PublisherId,
                u.UserName AS PublisherName,
                b.Title,
                b.AuthorName,
                b.CoverImagePath,
                b.Status AS BookStatus,
                b.WritingStatus,
                b.Language,
                b.AgeRating,
                b.Views,
                b.Rating,
                ISNULL(ch.ChaptersCount, 0) AS ChaptersCount,
                ISNULL(ch.SymbolsCount, 0) AS SymbolsCount,
                b.CreatedAt
            FROM UserLibraryListBooks lb
            JOIN Books b ON b.BookId = lb.BookId
            JOIN Users u ON u.UserId = b.PublisherId
            OUTER APPLY
            (
                SELECT COUNT(*) AS ChaptersCount, ISNULL(SUM(LEN(c.Text)), 0) AS SymbolsCount
                FROM Chapters c
                WHERE c.BookId = b.BookId
            ) ch
            WHERE lb.UserId = @UserId AND lb.ListId = @ListId
            ORDER BY lb.AddedAt DESC;";

            await using var connection = SqlConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@ListId", listId);

            var result = new List<BookListItem>();
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new BookListItem
                {
                    BookId = reader.GetInt32(reader.GetOrdinal("BookId")),
                    PublisherId = reader.GetInt32(reader.GetOrdinal("PublisherId")),
                    PublisherName = reader.GetString(reader.GetOrdinal("PublisherName")),
                    Title = reader.GetNullableString("Title"),
                    AuthorName = reader.GetNullableString("AuthorName"),
                    CoverImagePath = reader.GetNullableString("CoverImagePath"),
                    BookStatus = (BookStatus)reader.GetInt32(reader.GetOrdinal("BookStatus")),
                    WritingStatus = (WritingStatus)reader.GetInt32(reader.GetOrdinal("WritingStatus")),
                    Language = (Language)reader.GetInt32(reader.GetOrdinal("Language")),
                    AgeRating = reader.GetInt32(reader.GetOrdinal("AgeRating")),
                    Views = reader.GetInt64Safe("Views"),
                    Rating = reader.GetDoubleSafe("Rating"),
                    ChaptersCount = reader.GetInt32(reader.GetOrdinal("ChaptersCount")),
                    SymbolsCount = reader.GetInt32(reader.GetOrdinal("SymbolsCount"))
                });
            }

            return result;
        }

        public async Task AddBookToListAsync(int userId, int listId, int bookId)
        {
            const string sql = @"
            IF NOT EXISTS
            (
                SELECT 1
                FROM UserLibraryListBooks
                WHERE UserId = @UserId AND ListId = @ListId AND BookId = @BookId
            )
            BEGIN
                INSERT INTO UserLibraryListBooks (UserId, ListId, BookId, AddedAt)
                VALUES (@UserId, @ListId, @BookId, SYSDATETIME())
            END";

            await using var connection = SqlConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@ListId", listId);
            command.Parameters.AddWithValue("@BookId", bookId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task RemoveBookFromListAsync(int userId, int listId, int bookId)
        {
            const string sql = @"
            DELETE FROM UserLibraryListBooks
            WHERE UserId = @UserId AND ListId = @ListId AND BookId = @BookId;";

            await using var connection = SqlConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@ListId", listId);
            command.Parameters.AddWithValue("@BookId", bookId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task AddToFavoritesAsync(int userId, int bookId)
        {
            const string sql = @"
            IF NOT EXISTS (SELECT 1 FROM Favorites WHERE UserId = @UserId AND BookId = @BookId)
            BEGIN
                INSERT INTO Favorites (UserId, BookId, AddedAt)
                VALUES (@UserId, @BookId, SYSDATETIME())
            END";

            await using var connection = SqlConnectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@BookId", bookId);
            await command.ExecuteNonQueryAsync();
        }

        public async Task RemoveFromFavoritesAsync(int userId, int bookId)
        {
            const string sql = "DELETE FROM Favorites WHERE UserId = @UserId AND BookId = @BookId";

            await using var connection = SqlConnectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@BookId", bookId);
            await command.ExecuteNonQueryAsync();
        }

        public async Task<bool> IsBookFavoriteAsync(int userId, int bookId)
        {
            const string sql = "SELECT COUNT(1) FROM Favorites WHERE UserId = @UserId AND BookId = @BookId";

            await using var connection = SqlConnectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@BookId", bookId);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }

        public async Task AddToLibraryAsync(int userId, int bookId)
        {
            await using var connection = SqlConnectionFactory.CreateConnection();

            const string query = """
                INSERT INTO UserLibrary (UserId, BookId, AddedAt)
                VALUES (@UserId, @BookId, @AddedAt)
            """;

            using var command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@BookId", bookId);
            command.Parameters.AddWithValue("@AddedAt", DateTime.UtcNow);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task RemoveFromLibraryAsync(int userId, int bookId)
        {
            await using var connection = SqlConnectionFactory.CreateConnection();

            const string query = """
                DELETE FROM UserLibrary
                WHERE UserId = @UserId
                  AND BookId = @BookId
            """;

            using var command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@BookId", bookId);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task<bool> IsBookInLibraryAsync(int userId, int bookId)
        {
            await using var connection = SqlConnectionFactory.CreateConnection();

            const string query = """
                SELECT COUNT(1)
                FROM UserLibrary
                WHERE UserId = @UserId
                  AND BookId = @BookId
            """;

            using var command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@BookId", bookId);

            await connection.OpenAsync();

            var result = await command.ExecuteScalarAsync();

            return Convert.ToInt32(result) > 0;
        }
    }
}