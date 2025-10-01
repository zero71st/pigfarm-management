using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PigFarmManagement.Server.Infrastructure.Data;

public class PigFarmDbContextFactory : IDesignTimeDbContextFactory<PigFarmDbContext>
{
    public PigFarmDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PigFarmDbContext>();

        // Allow overriding via environment variable for CI or custom paths
        var connectionString = Environment.GetEnvironmentVariable("PIGFARM_CONNECTION")
                               ?? "Data Source=pigfarm.db";

        optionsBuilder.UseSqlite(connectionString);

        return new PigFarmDbContext(optionsBuilder.Options);
    }
}
