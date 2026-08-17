using Npgsql;

namespace RepyPharma.Data;

public static class DatabaseConnectionString
{
    private static readonly string[] CandidateKeys =
    [
        "ConnectionStrings:DefaultConnection",
        "AIVEN_DATABASE_URL",
        "DATABASE_URL",
        "POSTGRES_URL"
    ];

    public static string GetRequired(IConfiguration configuration)
    {
        foreach (var key in CandidateKeys)
        {
            var rawConnectionString = key == "ConnectionStrings:DefaultConnection"
                ? configuration.GetConnectionString("DefaultConnection")
                : configuration[key];

            if (string.IsNullOrWhiteSpace(rawConnectionString))
                continue;

            var connectionString = Normalize(rawConnectionString);
            ValidateHost(connectionString, key);

            return connectionString;
        }

        throw new InvalidOperationException(
            "Connection string do PostgreSQL não configurada. Configure ConnectionStrings__DefaultConnection no Render ou informe uma URI postgres:// em AIVEN_DATABASE_URL.");
    }

    private static string Normalize(string rawConnectionString)
    {
        var connectionString = rawConnectionString.Trim();

        if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "postgres" && uri.Scheme != "postgresql"))
        {
            return connectionString;
        }

        var userInfo = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(0) ?? string.Empty),
            Password = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(1) ?? string.Empty),
            SslMode = SslMode.Require
        };

        return builder.ConnectionString;
    }

    private static void ValidateHost(string connectionString, string sourceKey)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        if (string.IsNullOrWhiteSpace(builder.Host))
        {
            throw new InvalidOperationException(
                $"A connection string em {sourceKey} não possui Host configurado.");
        }

        if (builder.Host.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException(
                $"O Host da connection string em {sourceKey} contém espaço ou quebra de linha. Informe o host do Aiven em uma única linha.");
        }
    }
}
