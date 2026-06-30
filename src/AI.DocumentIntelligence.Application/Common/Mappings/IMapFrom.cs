using AutoMapper;

namespace AI.DocumentIntelligence.Application.Common.Mappings;

/// <summary>
/// Implemented by a DTO/contract to declare an AutoMapper mapping from <typeparamref name="TSource"/>.
/// <see cref="MappingProfile"/> discovers implementations by assembly scanning and wires them up,
/// so simple maps need no explicit <see cref="Profile"/> entry. Override <see cref="Mapping"/> to
/// customise member configuration.
/// </summary>
/// <typeparam name="TSource">The type to map from (typically a domain entity).</typeparam>
public interface IMapFrom<TSource>
{
    /// <summary>Configures the mapping. The default creates a straightforward member map.</summary>
    /// <param name="profile">The profile being built.</param>
    public void Mapping(Profile profile) => profile.CreateMap(typeof(TSource), GetType());
}
