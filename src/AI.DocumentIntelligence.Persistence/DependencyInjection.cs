using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Abstractions.Search;
using AI.DocumentIntelligence.Persistence.Context;
using AI.DocumentIntelligence.Persistence.HealthChecks;
using AI.DocumentIntelligence.Persistence.Repositories;
using AI.DocumentIntelligence.Persistence.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AI.DocumentIntelligence.Persistence;

/// <summary>
/// Wires all Persistence-layer services into the DI container. Called once at API startup.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers EF Core, repositories, the Unit of Work, and — when Azure AI Search is not
    /// configured — the pgvector-backed <see cref="ISearchService"/>.
    ///
    /// Search provider selection rules (Open/Closed — no code change required to switch):
    /// <list type="bullet">
    ///   <item><c>AzureSearch:Endpoint</c> present  →  Infrastructure's <c>AzureSearchService</c>
    ///     is used (registered by <c>AddInfrastructure</c>).</item>
    ///   <item><c>AzureSearch:Endpoint</c> absent / blank  →  <see cref="PgVectorSearchService"/>
    ///     is registered here, overriding the Azure registration.</item>
    /// </list>
    ///
    /// <c>AddPersistence</c> is called after <c>AddInfrastructure</c> in Program.cs, so the
    /// override is applied last and wins at DI resolution time.
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
                options.UseNpgsql(
                    connectionString,
                    npgsql => npgsql.UseVector());
            }
            else
            {
                // When no real connection string is configured (e.g., integration tests with
                // in-memory fakes), configure a placeholder so the DI graph compiles.
                options.UseNpgsql(
                    "Host=localhost;Database=placeholder;Username=placeholder;Password=placeholder",
                    npgsql => npgsql.UseVector());
            }
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IChatSessionRepository, ChatSessionRepository>();

        // Generic IRepository<T> — resolves Repository<T> for any entity type.
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // ---- Search provider selection ----
        // Infrastructure (AddInfrastructure, called before this) always registers
        // AzureSearchService as ISearchService.  When AzureSearch:Endpoint is blank the
        // Azure SDK would fail at runtime, so we replace it with PgVectorSearchService.
        //
        // PgVectorSearchService is registered as a Singleton and uses IServiceScopeFactory
        // to create a fresh scoped AppDbContext for each operation — the correct pattern
        // for singleton services that depend on scoped EF Core contexts.
        //
        // To switch to Azure AI Search: set AzureSearch:Endpoint and AzureSearch:ApiKey
        // in configuration. No code change is required (Open/Closed Principle).
        var azureSearchEndpoint = configuration["AzureSearch:Endpoint"];
        if (string.IsNullOrWhiteSpace(azureSearchEndpoint))
        {
            // Remove whatever ISearchService was registered by Infrastructure.
            services.RemoveAll<ISearchService>();

            services.AddSingleton<ISearchService, PgVectorSearchService>();
        }

        // ---- Health checks ----
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>(
                "database",
                tags: ["ready", "db"]);

        return services;
    }
}
