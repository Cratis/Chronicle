// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Extension methods for comparing <see cref="ReadModelDefinition"/> by value.
/// </summary>
public static class ReadModelDefinitionExtensions
{
    /// <summary>
    /// Determines whether two definitions describe the same read model by value.
    /// </summary>
    /// <remarks>
    /// The record's own equality compares the schema dictionary and the index collection by reference, so two
    /// definitions that arrive separately over the wire are never equal. A reconnecting client re-sends every
    /// definition freshly deserialized, which would make every re-registration look like a change.
    /// </remarks>
    /// <param name="definition">The definition.</param>
    /// <param name="other">The definition to compare with.</param>
    /// <returns>True if equivalent, false if not.</returns>
    public static bool IsEquivalentTo(this ReadModelDefinition definition, ReadModelDefinition other)
    {
        if (ReferenceEquals(definition, other))
        {
            return true;
        }

        if (definition.Identifier != other.Identifier ||
            definition.ContainerName != other.ContainerName ||
            definition.DisplayName != other.DisplayName ||
            definition.Owner != other.Owner ||
            definition.Source != other.Source ||
            definition.ObserverType != other.ObserverType ||
            definition.ObserverIdentifier != other.ObserverIdentifier ||
            definition.Sink != other.Sink ||
            !definition.Indexes.SequenceEqual(other.Indexes) ||
            definition.Schemas.Count != other.Schemas.Count)
        {
            return false;
        }

        foreach (var (generation, schema) in definition.Schemas)
        {
            if (!other.Schemas.TryGetValue(generation, out var otherSchema) ||
                !string.Equals(schema.ToJson(), otherSchema.ToJson(), StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
