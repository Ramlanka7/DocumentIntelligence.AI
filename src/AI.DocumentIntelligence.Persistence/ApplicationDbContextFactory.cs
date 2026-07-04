using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AI.DocumentIntelligence.Persistence;

/// <summary>
/// Design-time factory for <see cref="ApplicationDbContext"/>, used by the EF Core CLI tools
/// (<c>dotnet ef migrations add</c> etc.) when no live application service provider is available.
/// </summary>
internal sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // Design-time connection: read from env (set DB_CONNECTION for CI/CD) or fall back to local dev defaults.
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=documentintelligence;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString, npgsql => npgsql.UseVector());

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
