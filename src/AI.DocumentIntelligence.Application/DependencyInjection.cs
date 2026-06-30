using System.Reflection;
using AI.DocumentIntelligence.Application.Common.Behaviors;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AI.DocumentIntelligence.Application;

/// <summary>
/// Composition root for the Application layer. Registers MediatR (with the validation, logging,
/// performance and unhandled-exception pipeline behaviors), FluentValidation validators and the
/// AutoMapper profiles found in this assembly.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Registers all Application-layer services into the container.</summary>
    /// <param name="services">The service collection to add to.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        Assembly applicationAssembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(applicationAssembly);

            // Order matters: behaviors execute outermost-first, so exception handling wraps
            // everything, then logging, then timing, with validation closest to the handler.
            configuration.AddOpenBehavior(typeof(UnhandledExceptionBehavior<,>));
            configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
            configuration.AddOpenBehavior(typeof(PerformanceBehavior<,>));
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(applicationAssembly, includeInternalTypes: true);
        services.AddAutoMapper(cfg => cfg.AddMaps(applicationAssembly));

        return services;
    }
}
