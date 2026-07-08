namespace RangeOps.Core.Data;

/// <summary>
/// Resolves the Npgsql connection string, in priority order:
///   1. Explicit environment variables (local dev, CI, or pointing at a
///      specific database) — used whenever POSTGRES_HOST is set.
///   2. A build-time default baked into release builds (<see cref="BuildDefaults"/>),
///      which ships the shared hosted database so downloaded consoles "just work."
///   3. The local docker-compose database, as a final fallback.
/// </summary>
public static class DbConfig
{
    private static string? Env(string key) =>
        Environment.GetEnvironmentVariable(key) is { Length: > 0 } v ? v : null;

    public static string ConnectionString
    {
        get
        {
            // 1. Explicit env config wins.
            if (Env("POSTGRES_HOST") is { } host)
                return $"Host={host};" +
                       $"Port={Env("POSTGRES_PORT") ?? "5544"};" +
                       $"Database={Env("POSTGRES_DB") ?? "rangeops"};" +
                       $"Username={Env("POSTGRES_USER") ?? "rangeops"};" +
                       $"Password={Env("POSTGRES_PASSWORD") ?? "rangeops"}";

            // 2. Shipped default (release builds inject the shared DB here).
            if (!string.IsNullOrWhiteSpace(BuildDefaults.DefaultDbConnection))
                return BuildDefaults.DefaultDbConnection;

            // 3. Local docker-compose fallback.
            return "Host=localhost;Port=5544;Database=rangeops;Username=rangeops;Password=rangeops";
        }
    }
}
