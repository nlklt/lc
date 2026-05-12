using lc.Data.Repositories.Interfaces;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Infrastructure.Repositories.Sql;
using lc.Models;
using lc.Models.Enums;
using Microsoft.Data.SqlClient;
using System.Net;
using System.Text;

namespace lc.Data.Repositories
{
    public sealed class BookRepository(
        IChapterRepository chapterRepository, 
        ICommentRepository commentRepository,
        ITagRepository tagRepository, 
        ICategoryRepository categoryRepository
        ) : IBookRepository
    {
        private readonly IChapterRepository _chapterRepository = chapterRepository;
        private readonly ICommentRepository _commentRepository = commentRepository;

        public async Task<Book?> GetByIdAsync(int bookId, bool includeChapters = false, bool includeComments = false)
        {
            const string sql = @"
            SELECT 
                b.BookId, b.PublisherId, b.Title, b.AuthorName, b.Description, b.CoverImagePath,
                b.BookStatus, b.WritingStatus, b.Language, b.AgeRating, b.SymbolsCount, 
                b.ChaptersCount, b.Views, b.Rating, b.CreatedAt, b.UpdatedAt,
                u.UserId AS PublisherUserId, u.UserName AS PublisherUserName, u.AvatarPath AS PublisherAvatarPath
            FROM Books b
            JOIN Users u ON u.UserId = b.PublisherId
            WHERE b.BookId = @BookId;

            SELECT t.TagId, t.Name FROM BookTags bt JOIN Tags t ON t.TagId = bt.TagId WHERE bt.BookId = @BookId ORDER BY t.Name;
            SELECT c.CategoryId, c.Name FROM BookCategories bc JOIN Categories c ON c.CategoryId = bc.CategoryId WHERE bc.BookId = @BookId ORDER BY c.Name;";

            try
            {
                await using var connection = SqlConnectionFactory.CreateConnection();
                await connection.OpenAsync();

                await using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@BookId", bookId);

                await using var reader = await command.ExecuteReaderAsync();

                if (!await reader.ReadAsync()) return null;

                var book = new Book
                {
                    BookId = reader.GetInt32(reader.GetOrdinal("BookId")),
                    PublisherId = reader.GetInt32(reader.GetOrdinal("PublisherId")),
                    Publisher = new User
                    {
                        UserId = reader.GetInt32(reader.GetOrdinal("PublisherUserId")),
                        UserName = reader.IsDBNull(reader.GetOrdinal("PublisherUserName")) ? "Unknown" : reader.GetString(reader.GetOrdinal("PublisherUserName"))
                    },
                    Title = reader.IsDBNull(reader.GetOrdinal("Title")) ? "" : reader.GetString(reader.GetOrdinal("Title")),
                    AuthorName = reader.IsDBNull(reader.GetOrdinal("AuthorName")) ? "" : reader.GetString(reader.GetOrdinal("AuthorName")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? "" : reader.GetString(reader.GetOrdinal("Description")),
                    CoverImagePath = reader.IsDBNull(reader.GetOrdinal("CoverImagePath")) ? null : reader.GetString(reader.GetOrdinal("CoverImagePath")),
                    BookStatus = (BookStatus)reader.GetInt32(reader.GetOrdinal("BookStatus")),
                    WritingStatus = (WritingStatus)reader.GetInt32(reader.GetOrdinal("WritingStatus")),
                    Language = (Language)reader.GetInt32(reader.GetOrdinal("Language")),
                    AgeRating = reader.GetInt32(reader.GetOrdinal("AgeRating")),
                    SymbolsCount = (int)reader.GetInt64(reader.GetOrdinal("SymbolsCount")),
                    ChaptersCount = reader.GetInt32(reader.GetOrdinal("ChaptersCount")),
                    Views = reader.GetInt32(reader.GetOrdinal("Views")),
                    Rating = (double)reader.GetDecimal(reader.GetOrdinal("Rating")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                    Tags = new List<Tag>(),
                    Categories = new List<Category>(),
                };

                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        book.Tags.Add(new Tag
                        {
                            TagId = reader.GetInt32(0),
                            Name = reader.GetString(1)
                        });
                    }
                }

                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        book.Categories.Add(new Category
                        {
                            CategoryId = reader.GetInt32(0),
                            Name = reader.GetString(1)
                        });
                    }
                }

                if (includeChapters) book.Chapters = [.. await _chapterRepository.GetByBookIdAsync(book.BookId)];
                if (includeComments) book.Comments = [.. await _commentRepository.GetByBookIdAsync(book.BookId)];

                return book;
            }
            catch (Exception) { throw; }
        }

        public async Task<int> CreateAsync(Book book)
        {
            const string sql = @"
            INSERT INTO Books
            (Title, Publisher, AuthorName, Description, CoverImagePath, BookStatus, Language, AgeRating, SymbolsCount, ChaptersCount, Views, Rating, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.BookId
            VALUES
            (@Title, @Publisher, @AuthorName, @Description, @CoverImagePath, @BookStatus, @Language, @AgeRating, @Views, @Rating, @CreatedAt, @UpdatedAt);";

            await using var connection = SqlConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            AddBookParameters(command, book);

            var result = await command.ExecuteScalarAsync();
            return result is int ChapterId ? ChapterId : 0;
        }

        public async Task UpdateAsync(Book book)
        {
            const string sql = @"
            UPDATE Books
            SET
                Title = @Title,
                Publisher = @Publisher,
                AuthorName = @AuthorName,
                Description = @Description,
                CoverImagePath = @CoverImagePath,
                PublisherId INT NOT NULL,
                BookStatus INT NOT NULL,
                WritingStatus INT NOT NULL,
                Language = @Language,
                AgeRating = @AgeRating,
                SymbolsCount = @SymbolsCount, 
                ChaptersCount = @ChaptersCount,
                Views = @Views,
                Rating = @Rating,
                UpdatedAt = @UpdatedAt
            WHERE BookId = @BookId;";

            await using var connection = SqlConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@BookId", book.BookId);
            AddBookParameters(command, book);

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(int bookId)
        {
            const string sql = @"DELETE FROM Books WHERE BookId = @BookId;";

            await using var connection = SqlConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@BookId", bookId);

            await command.ExecuteNonQueryAsync();
        }

        private static void AddBookParameters(SqlCommand command, Book book)
        {
            command.Parameters.AddWithValue("@Title", (object?)book.Title ?? DBNull.Value);
            command.Parameters.AddWithValue("@Publisher", (object?)book.Publisher ?? DBNull.Value);
            command.Parameters.AddWithValue("@AuthorName", (object?)book.AuthorName ?? DBNull.Value);
            command.Parameters.AddWithValue("@Description", (object?)book.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@CoverImagePath", (object?)book.CoverImagePath ?? DBNull.Value);
            command.Parameters.AddWithValue("@BookStatus", (int)book.BookStatus);
            command.Parameters.AddWithValue("@Language", (int)book.Language);
            command.Parameters.AddWithValue("@AgeRating", book.AgeRating);
            command.Parameters.AddWithValue("@Views", book.Views);
            command.Parameters.AddWithValue("@Rating", book.Rating);
            command.Parameters.AddWithValue("@CreatedAt", book.CreatedAt);
            command.Parameters.AddWithValue("@UpdatedAt", book.UpdatedAt);
        }

        public async Task<IReadOnlyList<BookListItem>> SearchAsync(BookFilterCriteria criteria)
        {
            var sql = new StringBuilder(@"
            SELECT
                b.BookId,
                b.PublisherId,
                u.UserName AS PublisherName,
                b.Title,
                b.AuthorName,
                b.CoverImagePath,
                b.BookStatus,
                b.WritingStatus,
                b.Language,
                b.AgeRating,
                b.Views,
                b.Rating,
                ISNULL(ch.ChaptersCount, 0) AS ChaptersCount,
                ISNULL(ch.SymbolsCount, 0) AS SymbolsCount,
                tg.TagsData,
                cg.CategoriesData
            FROM Books b
            JOIN Users u ON u.UserId = b.PublisherId
            OUTER APPLY
            (
                SELECT 
                    COUNT(*) AS ChaptersCount,
                    ISNULL(SUM(LEN(c.Text)), 0) AS SymbolsCount
                FROM Chapters c
                WHERE c.BookId = b.BookId
            ) ch
            OUTER APPLY
            (
                SELECT STRING_AGG(CONCAT(t.TagId, N'|', t.Name), N';') WITHIN GROUP (ORDER BY t.Name) AS TagsData
                FROM BookTags bt
                JOIN Tags t ON t.TagId = bt.TagId
                WHERE bt.BookId = b.BookId
            ) tg
            OUTER APPLY
            (
                SELECT STRING_AGG(CONCAT(ca.CategoryId, N'|', ca.Name), N';') WITHIN GROUP (ORDER BY ca.Name) AS CategoriesData
                FROM BookCategories bc
                JOIN Categories ca ON ca.CategoryId = bc.CategoryId
                WHERE bc.BookId = b.BookId
            ) cg
            WHERE 1 = 1
            ");

            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(criteria.SearchText))
            {
                sql.AppendLine(@"
                AND (
                    b.Title LIKE @SearchText OR
                    b.AuthorName LIKE @SearchText
                )");
                parameters.Add(new SqlParameter("@SearchText", $"%{criteria.SearchText.Trim()}%"));
            }

            AddIncludeExcludeFilter(sql, parameters, "b.BookStatus", criteria.IncludeBookStatuses, criteria.ExcludeBookStatuses, "BookStatus");

            AddIncludeExcludeFilter(sql, parameters, "b.WritingStatus", criteria.IncludeWritingStatuses, criteria.ExcludeWritingStatuses, "WritingStatus");

            AddIncludeExcludeFilter(sql, parameters, "b.Language", criteria.IncludeLanguages, criteria.ExcludeLanguages, "Language");

            AddIncludeExcludeFilter(sql, parameters, "b.AgeRating", criteria.IncludeAgeRatings, criteria.ExcludeAgeRatings, "AgeRating");

            if (criteria.RatingFrom.HasValue)
            {
                sql.AppendLine("AND b.Rating >= @RatingFrom");
                parameters.Add(new SqlParameter("@RatingFrom", criteria.RatingFrom.Value));
            }

            if (criteria.RatingTo.HasValue)
            {
                sql.AppendLine("AND b.Rating <= @RatingTo");
                parameters.Add(new SqlParameter("@RatingTo", criteria.RatingTo.Value));
            }

            if (criteria.CreatedFrom.HasValue)
            {
                sql.AppendLine("AND b.CreatedAt >= @CreatedFrom");
                parameters.Add(new SqlParameter("@CreatedFrom", criteria.CreatedFrom.Value));
            }

            if (criteria.CreatedTo.HasValue)
            {
                sql.AppendLine("AND b.CreatedAt <= @CreatedTo");
                parameters.Add(new SqlParameter("@CreatedTo", criteria.CreatedTo.Value));
            }

            if (criteria.ChaptersFrom.HasValue)
            {
                sql.AppendLine("AND ISNULL(ch.ChaptersCount, 0) >= @ChaptersFrom");
                parameters.Add(new SqlParameter("@ChaptersFrom", criteria.ChaptersFrom.Value));
            }

            if (criteria.ChaptersTo.HasValue)
            {
                sql.AppendLine("AND ISNULL(ch.ChaptersCount, 0) <= @ChaptersTo");
                parameters.Add(new SqlParameter("@ChaptersTo", criteria.ChaptersTo.Value));
            }

            if (criteria.SymbolsFrom.HasValue)
            {
                sql.AppendLine("AND ISNULL(ch.SymbolsCount, 0) >= @SymbolsFrom");
                parameters.Add(new SqlParameter("@SymbolsFrom", criteria.SymbolsFrom.Value));
            }

            if (criteria.SymbolsTo.HasValue)
            {
                sql.AppendLine("AND ISNULL(ch.SymbolsCount, 0) <= @SymbolsTo");
                parameters.Add(new SqlParameter("@SymbolsTo", criteria.SymbolsTo.Value));
            }

            AddTagFilter(sql, parameters, criteria);
            AddCategoryFilter(sql, parameters, criteria);

            sql.AppendLine($"ORDER BY {BuildOrderBy(criteria)};");

            await using var connection = SqlConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql.ToString(), connection);
            command.Parameters.AddRange(parameters.ToArray());

            var result = new List<BookListItem>();

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(MapBookListItem(reader));
            }

            return result;
        }

        private void AddIncludeExcludeFilter<T>(
            StringBuilder sql,
            List<SqlParameter> parameters,
            string columnName,
            IReadOnlyList<T> includeList,
            IReadOnlyList<T> excludeList,
            string paramNamePrefix
            )
        {
            if (includeList != null && includeList.Count > 0)
            {
                var inList = BuildInList(includeList, parameters, $"{paramNamePrefix}Inc");
                sql.AppendLine($"AND {columnName} IN ({inList})");
            }

            if (excludeList != null && excludeList.Count > 0)
            {
                var notInList = BuildInList(excludeList, parameters, $"{paramNamePrefix}Exc");
                sql.AppendLine($"AND {columnName} NOT IN ({notInList})");
            }
        }

        private static void AddTagFilter(StringBuilder sql, List<SqlParameter> parameters, BookFilterCriteria criteria)
        {
            if (criteria.ExcludeTagIds.Count > 0)
            {
                var pName = $"@ExcludeTags";
                sql.AppendLine($@"
            AND NOT EXISTS (
                SELECT 1 FROM BookTags bt 
                WHERE bt.BookId = b.BookId 
                AND bt.TagId IN ({BuildInList(criteria.ExcludeTagIds, parameters, "ExTag")})
            )");
            }

            if (criteria.IncludeTagIds.Count > 0)
            {
                if (criteria.StrictTagMatch)
                {
                    foreach (var tagId in criteria.IncludeTagIds)
                    {
                        var pName = $"@IncTag{tagId}";
                        sql.AppendLine($"AND EXISTS (SELECT 1 FROM BookTags WHERE BookId = b.BookId AND TagId = {pName})");
                        parameters.Add(new SqlParameter(pName, tagId));
                    }
                }
                else
                {
                    sql.AppendLine($@"
                AND EXISTS (
                    SELECT 1 FROM BookTags bt 
                    WHERE bt.BookId = b.BookId 
                    AND bt.TagId IN ({BuildInList(criteria.IncludeTagIds, parameters, "IncTag")})
                )");
                }
            }
        }

        private static void AddCategoryFilter(StringBuilder sql, List<SqlParameter> parameters, BookFilterCriteria criteria)
        {
            if (criteria.ExcludeCategoryIds.Count > 0)
            {
                var pName = $"@ExcludeCategories";
                sql.AppendLine($@"
            AND NOT EXISTS (
                SELECT 1 FROM BookCategories bt 
                WHERE bt.BookId = b.BookId 
                AND bt.CategoryId IN ({BuildInList(criteria.ExcludeCategoryIds, parameters, "ExCategory")})
            )");
            }

            if (criteria.IncludeCategoryIds.Count > 0)
            {
                if (criteria.StrictCategoryMatch)
                {
                    foreach (var tagId in criteria.IncludeCategoryIds)
                    {
                        var pName = $"@IncCategory{tagId}";
                        sql.AppendLine($"AND EXISTS (SELECT 1 FROM BookCategories WHERE BookId = b.BookId AND CategoryId = {pName})");
                        parameters.Add(new SqlParameter(pName, tagId));
                    }
                }
                else
                {
                    sql.AppendLine($@"
                AND EXISTS (
                    SELECT 1 FROM BookCategories bt 
                    WHERE bt.BookId = b.BookId 
                    AND bt.CategoryId IN ({BuildInList(criteria.IncludeCategoryIds, parameters, "IncCategory")})
                )");
                }
            }
        }

        private static string BuildInList<T>(IReadOnlyList<T> values, List<SqlParameter> parameters, string prefix)
        {
            var names = new List<string>(values.Count);

            for (int i = 0; i < values.Count; i++)
            {
                var name = $"@{prefix}{i}";
                parameters.Add(new SqlParameter(name, values[i]!));
                names.Add(name);
            }

            return string.Join(", ", names);
        }

        private static string BuildOrderBy(BookFilterCriteria criteria)
        {
            string column = criteria.SortField switch
            {
                nameof(BookListItem.Title) => "b.Title",
                nameof(BookListItem.AuthorName) => "b.AuthorName",
                nameof(BookListItem.Rating) => "b.Rating",
                nameof(BookListItem.Views) => "b.Views",
                nameof(BookListItem.ChaptersCount) => "ISNULL(ch.ChaptersCount, 0)",
                nameof(BookListItem.SymbolsCount) => "ISNULL(ch.SymbolsCount, 0)",
                nameof(BookListItem.BookStatus) => "b.BookStatus",
                nameof(BookListItem.WritingStatus) => "b.WritingStatus",
                nameof(BookListItem.CreatedAt) => "b.CreatedAt",
                _ => "b.Title"
            };

            return $"{column} {(criteria.SortAscending ? "ASC" : "DESC")}";
        }

        private static BookListItem MapBookListItem(SqlDataReader reader)
        {
            return new BookListItem
            {
                BookId = reader.GetInt32(reader.GetOrdinal("BookId")),
                PublisherId = reader.GetInt32(reader.GetOrdinal("PublisherId")),
                PublisherName = reader.GetString(reader.GetOrdinal("PublisherName")),
                Title = reader.IsDBNull(reader.GetOrdinal("Title")) ? null : reader.GetString(reader.GetOrdinal("Title")),
                AuthorName = reader.IsDBNull(reader.GetOrdinal("AuthorName")) ? null : reader.GetString(reader.GetOrdinal("AuthorName")),
                CoverImagePath = reader.IsDBNull(reader.GetOrdinal("CoverImagePath")) ? null : reader.GetString(reader.GetOrdinal("CoverImagePath")),
                BookStatus = (BookStatus)reader.GetInt32(reader.GetOrdinal("BookStatus")),
                WritingStatus = (WritingStatus)reader.GetInt32(reader.GetOrdinal("WritingStatus")),
                Language = (Language)reader.GetInt32(reader.GetOrdinal("Language")),
                AgeRating = reader.GetInt32(reader.GetOrdinal("AgeRating")),
                Views = reader.GetInt32(reader.GetOrdinal("Views")),
                Rating = (double)reader.GetDecimal(reader.GetOrdinal("Rating")),
                ChaptersCount = reader.GetInt32(reader.GetOrdinal("ChaptersCount")),
                SymbolsCount = (int)reader.GetInt64(reader.GetOrdinal("SymbolsCount")),
                Tags = ParseTags(reader.IsDBNull(reader.GetOrdinal("TagsData")) ? null : reader.GetString(reader.GetOrdinal("TagsData"))),
                Categories = ParseCategories(reader.IsDBNull(reader.GetOrdinal("CategoriesData")) ? null : reader.GetString(reader.GetOrdinal("CategoriesData")))
            };
        }

        private static List<Tag> ParseTags(string? data)
        {
            var result = new List<Tag>();
            if (string.IsNullOrWhiteSpace(data))
                return result;

            foreach (var part in data.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var pieces = part.Split('|', 2);
                if (pieces.Length != 2)
                    continue;

                if (!int.TryParse(pieces[0], out var ChapterId))
                    continue;

                result.Add(new Tag { TagId = ChapterId, Name = pieces[1] });
            }

            return result;
        }

        private static List<Category> ParseCategories(string? data)
        {
            var result = new List<Category>();
            if (string.IsNullOrWhiteSpace(data))
                return result;

            foreach (var part in data.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var pieces = part.Split('|', 2);
                if (pieces.Length != 2)
                    continue;

                if (!int.TryParse(pieces[0], out var ChapterId))
                    continue;

                result.Add(new Category { CategoryId = ChapterId, Name = pieces[1] });
            }

            return result;
        }
    }
}
