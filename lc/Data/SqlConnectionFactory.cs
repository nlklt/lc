using System.Configuration;
using Microsoft.Data.SqlClient;

namespace lc.Data
{
    public static class SqlConnectionFactory
    {
        private static readonly string ConnectionString =
            ConfigurationManager.ConnectionStrings["eLibDb"].ConnectionString;

        public static SqlConnection CreateConnection()
            => new(ConnectionString);
    }
}