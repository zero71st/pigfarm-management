using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.AspNetCore.WebUtilities;

namespace PigFarmManagement.Server.Infrastructure.Data;

public class PigFarmDbContextFactory : IDesignTimeDbContextFactory<PigFarmDbContext>
{
    public PigFarmDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PigFarmDbContext>();

        // Allow overriding via DATABASE_URL (Railway) or PIGFARM_CONNECTION for CI; fall back to SQLite
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            // Normalize possible tcp:// urls to postgresql:// and parse DATABASE_URL (postgres://user:pass@host:port/dbname)
            if (databaseUrl.StartsWith("tcp://", StringComparison.OrdinalIgnoreCase))
            {
                databaseUrl = "postgresql://" + databaseUrl.Substring("tcp://".Length);
            }

            var uri = new Uri(databaseUrl);
            var userInfo = uri.UserInfo.Split(':', 2);

            // Allow overriding SSL mode via query string for local dev (e.g., ?sslmode=disable)
            // Defaults to Require (Railway expects SSL).
            var sslMode = Npgsql.SslMode.Require;
            try
            {
                var query = QueryHelpers.ParseQuery(uri.Query);
                if (query.TryGetValue("sslmode", out var sslModeValue))
                {
                    var raw = sslModeValue.ToString();
                    if (!string.IsNullOrWhiteSpace(raw) && Enum.TryParse<Npgsql.SslMode>(raw, ignoreCase: true, out var parsed))
                        sslMode = parsed;
                }
            }
            catch
            {
                // Ignore malformed query string and keep default SSL mode.
            }

            var npgsqlBuilder = new Npgsql.NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.Port > 0 ? uri.Port : 5432,
                Username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "",
                Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "",
                Database = uri.AbsolutePath.TrimStart('/'),
                SslMode = sslMode,
                Pooling = true
            };

            optionsBuilder.UseNpgsql(npgsqlBuilder.ToString());
            return new PigFarmDbContext(optionsBuilder.Options);
        }

        // Allow overriding via environment variable for CI or custom paths
        var connectionString = Environment.GetEnvironmentVariable("PIGFARM_CONNECTION")
                               ?? "Data Source=pigfarm.db";

        optionsBuilder.UseSqlite(connectionString);

        return new PigFarmDbContext(optionsBuilder.Options);
    }
}
