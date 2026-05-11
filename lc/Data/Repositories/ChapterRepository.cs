using lc.Infrastructure.Data;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
using Microsoft.Data.SqlClient;

namespace lc.Infrastructure.Repositories.Sql
{
    public sealed class ChapterRepository : IChapterRepository
    {
        public async Task<Chapter?> GetByIdAsync(int chapterId)
        {
            const string sql = @"
            SELECT ChapterId, BookId, ChapterNumber, Title, Text, CreatedAt, UpdatedAt
            FROM Chapters
            WHERE ChapterId = @ChapterId;";

            await using var connection = SqlConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ChapterId", chapterId);

            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return Map(reader);
        }

        public async Task<List<Chapter>> GetByBookIdAsync(int bookId)
        {
            const string sql = @"
            SELECT ChapterId, BookId, ChapterNumber, Title, Text, CreatedAt, UpdatedAt
            FROM Chapters
            WHERE BookId = @BookId
            ORDER BY ChapterNumber;";

            await using var connection = SqlConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@BookId", bookId);

            var result = new List<Chapter>();
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                result.Add(Map(reader));

            return result;
        }

        public async Task<int> CreateAsync(Chapter chapter)
        {
            const string sql = @"
            INSERT INTO Chapters
            (
                BookId, ChapterNumber, Title, Text,
                CreatedAt, UpdatedAt
            )
            OUTPUT INSERTED.ChapterId
            VALUES
            (
                @BookId, @ChapterNumber, @Title, @Text,
                @CreatedAt, @UpdatedAt
            );";

            await using var connection = SqlConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            AddParameters(command, chapter);

            var result = await command.ExecuteScalarAsync();
            return result is int ChapterId ? ChapterId : 0;
        }

        public async Task UpdateAsync(Chapter chapter)
        {
            const string sql = @"
            UPDATE Chapters
            SET
                BookId = @BookId,
                ChapterNumber = @ChapterNumber,
                Title = @Title,
                Text = @Text,
                UpdatedAt = @UpdatedAt
            WHERE ChapterId = @ChapterId;";

            await using var connection = SqlConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ChapterId", chapter.ChapterId);
            AddParameters(command, chapter);

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(int chapterId)
        {
            const string sql = @"DELETE FROM Chapters WHERE ChapterId = @ChapterId;";

            await using var connection = SqlConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ChapterId", chapterId);

            await command.ExecuteNonQueryAsync();
        }

        private static void AddParameters(SqlCommand command, Chapter chapter)
        {
            command.Parameters.AddWithValue("@BookId", chapter.BookId);
            command.Parameters.AddWithValue("@ChapterNumber", chapter.ChapterNumber);
            command.Parameters.AddWithValue("@Title", (object?)chapter.Title ?? DBNull.Value);
            command.Parameters.AddWithValue("@Text", chapter.Text);
            command.Parameters.AddWithValue("@CreatedAt", chapter.CreatedAt);
            command.Parameters.AddWithValue("@UpdatedAt", chapter.UpdatedAt);
        }

        private static Chapter Map(SqlDataReader reader)
        {
            return new Chapter
            {
                ChapterId = reader.GetInt32(reader.GetOrdinal("Id")),
                BookId = reader.GetInt32(reader.GetOrdinal("BookId")),
                ChapterNumber = reader.GetInt32(reader.GetOrdinal("ChapterNumber")),
                Title = reader.GetNullableString("Title"),
                Text = reader.GetString(reader.GetOrdinal("Text")),
                CreatedAt = reader.GetDateTimeSafe("CreatedAt"),
                UpdatedAt = reader.GetDateTimeSafe("UpdatedAt")
            };
        }
    }
}