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

        // PostgreSQL only. Requires DATABASE_URL:
        // - Railway-style URL: postgresql://user:password@host:port/database
        // - OR raw Npgsql connection string: Server=...;Port=...;Database=...;User Id=...;Password=...;
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (string.IsNullOrWhiteSpace(databaseUrl))
        {
            throw new InvalidOperationException(
                "DATABASE_URL is required for design-time DbContext creation. SQLite support has been removed.");
        }

        // If DATABASE_URL looks like a connection string, use it directly.
        if (databaseUrl.Contains("=", StringComparison.Ordinal) && !databaseUrl.Contains("://", StringComparison.Ordinal))
        {
            optionsBuilder.UseNpgsql(databaseUrl);
            return new PigFarmDbContext(optionsBuilder.Options);
        }

        // Normalize possible tcp:// urls to postgresql://
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
}
