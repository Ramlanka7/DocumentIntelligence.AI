using System.Reflection;
using AutoMapper;

namespace AI.DocumentIntelligence.Application.Common.Mappings;

/// <summary>
/// The Application layer's root AutoMapper profile. It scans this assembly for types implementing
/// <see cref="IMapFrom{TSource}"/> and applies each one's <see cref="IMapFrom{TSource}.Mapping"/>,
/// giving feature DTOs a single place to register entity↔DTO maps as the system grows.
/// </summary>
public sealed class MappingProfile : Profile
{
    /// <summary>Initializes the profile by applying every discovered <see cref="IMapFrom{TSource}"/>.</summary>
    public MappingProfile() => ApplyMappingsFromAssembly(Assembly.GetExecutingAssembly());

    private void ApplyMappingsFromAssembly(Assembly assembly)
    {
        const string mappingMethodName = nameof(IMapFrom<object>.Mapping);

        var implementingTypes = assembly.GetExportedTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                && Array.Exists(
                    t.GetInterfaces(),
                    i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMapFrom<>)))
            .ToList();

        foreach (Type type in implementingTypes)
        {
            foreach (Type mapFromInterface in type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMapFrom<>)))
            {
                Type sourceType = mapFromInterface.GetGenericArguments()[0];

                // Only instantiate when the type declares a custom Mapping override.
                // For the default case (just IMapFrom<T> with no override) we replicate the
                // default interface method directly — avoiding the need for a parameterless
                // constructor, which primary-constructor records do not have.
                MethodInfo? declared = type.GetMethod(
                    mappingMethodName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly,
                    binder: null,
                    types: [typeof(Profile)],
                    modifiers: null);

                if (declared is not null)
                {
                    object? instance = Activator.CreateInstance(type);
                    declared.Invoke(instance, [this]);
                }
                else
                {
                    CreateMap(sourceType, type);
                }
            }
        }
    }
}
