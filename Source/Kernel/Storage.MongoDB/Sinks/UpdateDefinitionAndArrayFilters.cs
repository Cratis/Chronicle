// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Represents a MongoDB update definition and accompanying array filters.
/// </summary>
/// <param name="UpdateDefinition">The actual update definition.</param>
/// <param name="ArrayFilters">Any array filters associated.</param>
/// <param name="hasChanges">Whether or not there are changes.</param>
public record UpdateDefinitionAndArrayFilters(UpdateDefinition<BsonDocument> UpdateDefinition, IEnumerable<BsonDocumentArrayFilterDefinition<BsonDocument>> ArrayFilters, bool hasChanges)
{
    /// <summary>
    /// Gets parent paths that may contain legacy BSON nulls and must be conditionally unset before the leaf update.
    /// </summary>
    internal IReadOnlyList<string> NullParentPaths { get; init; } = [];

    /// <summary>
    /// Gets indexed parents that may contain legacy BSON nulls, with per-element filters for their conditional unset.
    /// </summary>
    internal IReadOnlyList<NullArrayParent> NullArrayParents { get; init; } = [];

    /// <summary>
    /// Compares only the original positional values, excluding internal repair metadata.
    /// </summary>
    /// <param name="other">The value to compare with.</param>
    /// <returns>Whether the positional values are equal.</returns>
    public virtual bool Equals(UpdateDefinitionAndArrayFilters? other) =>
        other is not null &&
        EqualityContract == other.EqualityContract &&
        EqualityComparer<UpdateDefinition<BsonDocument>>.Default.Equals(UpdateDefinition, other.UpdateDefinition) &&
        EqualityComparer<IEnumerable<BsonDocumentArrayFilterDefinition<BsonDocument>>>.Default.Equals(ArrayFilters, other.ArrayFilters) &&
        hasChanges == other.hasChanges;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(EqualityContract, UpdateDefinition, ArrayFilters, hasChanges);
}
