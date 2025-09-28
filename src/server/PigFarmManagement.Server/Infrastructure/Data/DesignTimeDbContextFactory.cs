using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PigFarmManagement.Server.Infrastructure.Data;

/// <summary>
/// Design-time factory for PigFarmDbContext used by dotnet-ef to generate migrations.
/// Uses SQLite for design-time so migrations can be produced (InMemory provider doesn't support migrations).
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PigFarmDbContext>
{
    public PigFarmDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<PigFarmDbContext>();
        // Use a file-based sqlite DB under the server project for migrations generation
        builder.UseSqlite("Data Source=./migrations-dev.db");
        return new PigFarmDbContext(builder.Options);
    }
}
