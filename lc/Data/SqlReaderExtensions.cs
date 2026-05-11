using Microsoft.Data.SqlClient;
using System;

namespace lc.Infrastructure.Data
{
    internal static class SqlReaderExtensions
    {
        public static string? GetNullableString(this SqlDataReader reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }

        public static int? GetNullableInt32(this SqlDataReader reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
        }

        public static long GetInt64Safe(this SqlDataReader reader, string column)
        {
            return reader.GetInt64(reader.GetOrdinal(column));
        }

        public static double GetDoubleSafe(this SqlDataReader reader, string column)
        {
            return reader.GetDouble(reader.GetOrdinal(column));
        }

        public static DateTime GetDateTimeSafe(this SqlDataReader reader, string column)
        {
            return reader.GetDateTime(reader.GetOrdinal(column));
        }

        public static bool GetBooleanSafe(this SqlDataReader reader, string column)
        {
            return reader.GetBoolean(reader.GetOrdinal(column));
        }
    }
}