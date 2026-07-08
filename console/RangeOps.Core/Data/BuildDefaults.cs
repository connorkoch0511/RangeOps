namespace RangeOps.Core.Data;

/// <summary>
/// Build-time defaults. The release workflow overwrites this file (from a CI
/// secret) so distributed console builds ship with the shared hosted-database
/// connection string.
///
/// The committed value is intentionally EMPTY: no credentials live in the repo.
/// Local/dev builds therefore fall back to environment variables or the local
/// docker-compose database (see <see cref="DbConfig"/>).
/// </summary>
internal static class BuildDefaults
{
    public const string DefaultDbConnection = "";
}
