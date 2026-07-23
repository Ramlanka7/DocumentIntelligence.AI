using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Abstractions.Search;
using AI.DocumentIntelligence.Persistence.Context;
using AI.DocumentIntelligence.Persistence.HealthChecks;
using AI.DocumentIntelligence.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AI.DocumentIntelligence.Persistence;

/// <summary>
/// Wires all Persistence-layer services into the DI container. Called once at API startup.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers EF Core, repositories, and the Unit of Work.
    ///
    /// Document retrieval is not wired here — Azure AI Search is the sole
    /// <see cref="ISearchService"/> and is registered by <c>AddInfrastructure</c>.
    /// PostgreSQL stores only relational state: users, documents, sessions, audit logs
    /// and usage metrics.
    /// </summary>
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // AddDbContextPool reuses DbContext instances across requests instead of allocating new
        // ones, reducing GC pressure in high-throughput production scenarios. EF Core resets
        // the ChangeTracker and state between uses, so it is safe as long as the context has
        // no constructor dependencies beyond DbContextOptions — which AppDbContext does not.
        services.AddDbContextPool<AppDbContext>(options =>
        {
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseNpgsql(connectionString);
            }
            else
            {
                // When no real connection string is configured (e.g., integration tests with
                // in-memory fakes), configure a placeholder so the DI graph compiles.
                options.UseNpgsql(
                    "Host=localhost;Database=placeholder;Username=placeholder;Password=placeholder");
            }
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IChatSessionRepository, ChatSessionRepository>();

        // Generic IRepository<T> — resolves Repository<T> for any entity type.
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // ---- Health checks ----
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>(
                "database",
                tags: ["ready", "db"]);

        // ---- Data seeding ----
        // Creates the initial admin user (see DataSeeder). Invoked from Program.cs via
        // SeedDatabaseAsync when Database:SeedOnStartup is enabled.
        services.AddScoped<Seed.DataSeeder>();

        return services;
    }

    /// <summary>
    /// Runs the idempotent data seeder (initial admin user) in its own DI scope.
    /// Call after migrations have been applied. Gated in Program.cs by
    /// <c>Database:SeedOnStartup</c>.
    /// </summary>
    public static async Task SeedDatabaseAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<Seed.DataSeeder>();
        await seeder.SeedAsync(cancellationToken);
    }
}
