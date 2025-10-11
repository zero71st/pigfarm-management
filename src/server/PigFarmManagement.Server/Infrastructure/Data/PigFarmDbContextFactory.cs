using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

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
            var npgsqlBuilder = new Npgsql.NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.Port > 0 ? uri.Port : 5432,
                Username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "",
                Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "",
                Database = uri.AbsolutePath.TrimStart('/'),
                SslMode = Npgsql.SslMode.Require,
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
