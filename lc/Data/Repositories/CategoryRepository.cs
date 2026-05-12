using lc.Data;
using lc.Data.Repositories.Interfaces;
using lc.Models;
using Microsoft.Data.SqlClient;

namespace lc.Infrastructure.Repositories.Sql
{
    public sealed class CategoryRepository : ICategoryRepository
    {
        public async Task<IReadOnlyList<Category>> GetAllAsync()
        {
            const string sql = @"
            SELECT CategoryId, Name
            FROM Categories
            ORDER BY Name;";

            await using var connection = SqlConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            var result = new List<Category>();

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new Category
                {
                    CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                    Name = reader.GetString(reader.GetOrdinal("Name"))
                });
            }

            return result;
        }
    }
}