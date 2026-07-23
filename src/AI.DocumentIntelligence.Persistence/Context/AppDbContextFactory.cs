using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AI.DocumentIntelligence.Persistence.Context;

/// <summary>
/// Design-time factory for <see cref="AppDbContext"/>, used by the EF Core CLI tools
/// (<c>dotnet ef migrations add</c>) when no running host is available.
/// The connection string here is only for local development/migration generation;
/// runtime configuration comes from <see cref="DependencyInjection.AddPersistence"/>.
/// </summary>
internal sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=document_intelligence;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}
