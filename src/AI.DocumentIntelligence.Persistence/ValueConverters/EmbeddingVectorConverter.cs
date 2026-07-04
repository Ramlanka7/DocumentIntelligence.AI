using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Pgvector;

namespace AI.DocumentIntelligence.Persistence.ValueConverters;

/// <summary>
/// Converts between <see cref="IReadOnlyList{float}"/> (the domain type used in
/// <see cref="AI.DocumentIntelligence.Domain.Entities.DocumentChunk.Embedding"/>) and
/// <see cref="Vector"/> (the Pgvector database type stored in the <c>vector(1536)</c> column).
/// A <see langword="null"/> embedding round-trips as <see langword="null"/>.
/// </summary>
internal sealed class EmbeddingVectorConverter()
    : ValueConverter<IReadOnlyList<float>?, Vector?>(
        domainValue => domainValue == null
            ? null
            : new Vector(domainValue.ToArray()),
        dbValue => dbValue == null
            ? null
            : (IReadOnlyList<float>)dbValue.ToArray());
