using lc.Infrastructure.Data;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
using Microsoft.Data.SqlClient;

namespace lc.Infrastructure.Repositories.Sql
{
    public sealed class CommentRepository : ICommentRepository
    {
        public async Task<Comment?> GetByIdAsync(int commentId)
        {
            const string sql = @"
            SELECT
                c.CommentId, c.UserId, u.UserName, c.BookId,
                c.Text, c.CreatedAt, c.UpdatedAt
            FROM Comments c
            JOIN Users u ON u.UserId = c.UserId
            WHERE c.CommentId = @CommentId;";

            await using var connection = SqlConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@CommentId", commentId);

            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return Map(reader);
        }

        public async Task<IReadOnlyList<Comment>> GetByBookIdAsync(int bookId)
        {
            const string sql = @"
            SELECT
                c.CommentId, c.UserId, u.UserName, c.BookId,
                c.Text, c.CreatedAt, c.UpdatedAt
            FROM Comments c
            JOIN Users u ON u.UserId = c.UserId
            WHERE c.BookId = @BookId
            ORDER BY c.CreatedAt DESC;";

            await using var connection = SqlConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@BookId", bookId);

            var result = new List<Comment>();
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                result.Add(Map(reader));

            return result;
        }

        public async Task<int> CreateAsync(Comment comment)
        {
            const string sql = @"
            INSERT INTO Comments
            (
                UserId, BookId, ChapterId, Text,
                CreatedAt, UpdatedAt
            )
            OUTPUT INSERTED.CommentId
            VALUES
            (
                @UserId, @BookId, @Text,
                @CreatedAt, @UpdatedAt
            );";

            await using var connection = SqlConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            AddParameters(command, comment);

            var result = await command.ExecuteScalarAsync();
            return result is int ChapterId ? ChapterId : 0;
        }

        public async Task UpdateAsync(Comment comment)
        {
            const string sql = @"
            UPDATE Comments
            SET
                UserId = @UserId,
                BookId = @BookId,
                Text = @Text,
                UpdatedAt = @UpdatedAt
            WHERE CommentId = @CommentId;";

            await using var connection = SqlConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@CommentId", comment.CommentId);
            AddParameters(command, comment);

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(int commentId)
        {
            const string sql = @"DELETE FROM Comments WHERE CommentId = @CommentId;";

            await using var connection = SqlConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@CommentId", commentId);

            await command.ExecuteNonQueryAsync();
        }

        private static void AddParameters(SqlCommand command, Comment comment)
        {
            command.Parameters.AddWithValue("@UserId", comment.UserId);
            command.Parameters.AddWithValue("@BookId", comment.BookId);
            command.Parameters.AddWithValue("@Text", comment.Text);
            command.Parameters.AddWithValue("@CreatedAt", comment.CreatedAt);
            command.Parameters.AddWithValue("@UpdatedAt", comment.UpdatedAt);
        }

        private static Comment Map(SqlDataReader reader)
        {
            return new Comment
            {
                CommentId = reader.GetInt32(reader.GetOrdinal("CommentId")),
                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                User = new User
                {
                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                    UserName = reader.GetString(reader.GetOrdinal("UserName"))
                },
                BookId = reader.GetInt32(reader.GetOrdinal("BookId")),
                Text = reader.GetString(reader.GetOrdinal("Text")),
                CreatedAt = reader.GetDateTimeSafe("CreatedAt"),
                UpdatedAt = reader.GetDateTimeSafe("UpdatedAt")
            };
        }
    }
}