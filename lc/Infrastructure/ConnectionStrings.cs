using System.Configuration;

namespace lc.Infrastructure;

public static class ConnectionStrings
{
    public static string ELibDb =>
        ConfigurationManager.ConnectionStrings["eLibDb"]?.ConnectionString
        ?? throw new InvalidOperationException("Connection string 'eLibDb' not found.");
}