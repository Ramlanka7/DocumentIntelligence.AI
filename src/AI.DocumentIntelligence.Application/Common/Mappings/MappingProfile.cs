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

        List<Type> mapFromTypes = assembly.GetExportedTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .Where(type => Array.Exists(
                type.GetInterfaces(),
                @interface => @interface.IsGenericType
                    && @interface.GetGenericTypeDefinition() == typeof(IMapFrom<>)))
            .ToList();

        foreach (Type type in mapFromTypes)
        {
            object? instance = Activator.CreateInstance(type);

            MethodInfo? methodInfo = type.GetMethod(mappingMethodName)
                ?? type.GetInterface(typeof(IMapFrom<>).Name)?.GetMethod(mappingMethodName);

            methodInfo?.Invoke(instance, [this]);
        }
    }
}
