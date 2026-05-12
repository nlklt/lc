using lc.Data;
using lc.Data.Repositories.Interfaces;
using lc.Models;
using Microsoft.Data.SqlClient;

namespace lc.Infrastructure.Repositories.Sql
{
    public sealed class TagRepository : ITagRepository
    {
        public async Task<IReadOnlyList<Tag>> GetAllAsync()
        {
            const string sql = @"
            SELECT TagId, Name
            FROM Tags
            ORDER BY Name;";

            await using var connection = SqlConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            var result = new List<Tag>();

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new Tag
                {
                    TagId = reader.GetInt32(reader.GetOrdinal("TagId")),
                    Name = reader.GetString(reader.GetOrdinal("Name"))
                });
            }

            return result;
        }
    }
}