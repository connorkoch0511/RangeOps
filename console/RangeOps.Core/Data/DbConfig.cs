namespace RangeOps.Core.Data;

/// <summary>Builds the Npgsql connection string from environment variables,
/// falling back to the docker-compose defaults.</summary>
public static class DbConfig
{
    private static string Env(string key, string fallback) =>
        Environment.GetEnvironmentVariable(key) is { Length: > 0 } v ? v : fallback;

    public static string ConnectionString =>
        $"Host={Env("POSTGRES_HOST", "localhost")};" +
        $"Port={Env("POSTGRES_PORT", "5544")};" +
        $"Database={Env("POSTGRES_DB", "rangeops")};" +
        $"Username={Env("POSTGRES_USER", "rangeops")};" +
        $"Password={Env("POSTGRES_PASSWORD", "rangeops")}";
}
