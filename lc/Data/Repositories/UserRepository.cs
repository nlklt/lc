using lc.Infrastructure.Data;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
using lc.Models.Enums;
using lc.Services;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace lc.Data.Repositories
{
    public sealed class UserRepository : IUserRepository
    {
        public async Task<int> CreateAsync(User user)
        {
            const string sql = @"
INSERT INTO Users
(UserName, PasswordHash, AvatarPath, BlockedComments, CreatedAt, Role, PreferredLanguage, PreferredTheme)
OUTPUT INSERTED.UserId
VALUES
(@UserName, @PasswordHash, @AvatarPath, @BlockedComments, @CreatedAt, @Role, @PreferredLanguage, @PreferredTheme);";

            await using var connection = SqlConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            AddUserParameters(command, user);

            var result = await command.ExecuteScalarAsync();
            return result is int ChapterId ? ChapterId : 0;
        }

        public async Task UpdateAsync(User user)
        {
            const string sql = @"
UPDATE Users
SET UserName = @UserName,
    PasswordHash = @PasswordHash,
    AvatarPath = @AvatarPath,
    BlockedComments = @BlockedComments,
    Role = @Role,
    PreferredLanguage = @PreferredLanguage,
    PreferredTheme = @PreferredTheme
WHERE UserId = @UserId;";

            await using var connection = SqlConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", user.UserId);
            AddUserParameters(command, user);

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(int userId)
        {
            const string sql = @"DELETE FROM Users WHERE UserId = @UserId;";

            await using var connection = SqlConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<User?> GetByIdAsync(int userId)
        {
            const string sql = @"
SELECT UserId, UserName, PasswordHash, AvatarPath, BlockedComments, CreatedAt, Role, PreferredLanguage, PreferredTheme
FROM Users
WHERE UserId = @UserId;";

            await using var connection = SqlConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return MapUser(reader);
        }

        public async Task<User?> GetByUserNameAsync(string userName)
        {
            const string sql = @"
SELECT UserId, UserName, PasswordHash, AvatarPath, BlockedComments, CreatedAt, Role, PreferredLanguage, PreferredTheme
FROM Users
WHERE UserName = @UserName;";

            await using var connection = SqlConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserName", userName);

            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return MapUser(reader);
        }

        public async Task<bool> ExistsByUserNameAsync(string userName)
        {
            const string sql = @"SELECT COUNT(1) FROM Users WHERE UserName = @UserName;";

            await using var connection = SqlConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserName", userName);

            var result = await command.ExecuteScalarAsync();
            return result is int count && count > 0;
        }

        private static void AddUserParameters(SqlCommand command, User user)
        {
            command.Parameters.AddWithValue("@UserName", user.UserName);
            command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
            command.Parameters.AddWithValue("@AvatarPath", (object?)user.AvatarPath ?? DBNull.Value);
            command.Parameters.AddWithValue("@BlockedComments", user.BlockedComments);
            command.Parameters.AddWithValue("@CreatedAt", user.CreatedAt);
            command.Parameters.AddWithValue("@Role", (int)user.Role);
            command.Parameters.AddWithValue("@PreferredLanguage", (int)user.PreferredLanguage);
            command.Parameters.AddWithValue("@PreferredTheme", user.PreferredTheme);
        }

        private static User MapUser(SqlDataReader reader)
        {
            return new User
            {
                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                UserName = reader.GetString(reader.GetOrdinal("UserName")),
                PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
                AvatarPath = reader.GetNullableString("AvatarPath"),
                BlockedComments = reader.GetBooleanSafe("BlockedComments"),
                CreatedAt = reader.GetDateTimeSafe("CreatedAt"),
                Role = (UserRole)reader.GetInt32(reader.GetOrdinal("Role")),
                PreferredLanguage = (Language)reader.GetInt32(reader.GetOrdinal("PreferredLanguage")),
                PreferredTheme = reader.GetString(reader.GetOrdinal("PreferredTheme"))
            };
        }

        public async Task<User?> AuthenticateAsync(string userName, string password)
        {
            var user = await GetByUserNameAsync(userName);
            if (user == null)
                return null;

            if (!PasswordHasher.Verify(password, user.PasswordHash))
                return null;

            return user;
        }

        public async Task<User> RegisterAsync(string userName, string password)
        {
            var existing = await GetByUserNameAsync(userName);
            if (existing != null)
                throw new InvalidOperationException("Пользователь уже существует.");

            var user = new User
            {
                UserName = userName,
                PasswordHash = PasswordHasher.Hash(password),
                CreatedAt = DateTime.UtcNow,
                Role = UserRole.Reader,
                PreferredLanguage = Language.Русский,
                PreferredTheme = "Dark",
                BlockedComments = false
            };
            await CreateAsync(user);

            return user;
        }

        public async Task UpdateSettingsAsync(User user) { }
    }
}