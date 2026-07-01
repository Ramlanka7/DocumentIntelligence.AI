using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace AI.DocumentIntelligence.Persistence;

/// <summary>
/// Wires all Persistence-layer services into the DI container. Called once at API startup.
/// Stub implementations are registered here pending T02 (full EF Core setup).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        // T02 stubs — these will be replaced with real EF Core repositories once the
        // DbContext and migrations are in place.
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
