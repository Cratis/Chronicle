// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Patterns;
using KernelBehaviorPattern = Cratis.Chronicle.Concepts.Patterns.BehaviorPattern;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Patterns;

/// <summary>
/// Converts between <see cref="BehaviorPattern"/> and <see cref="KernelBehaviorPattern"/>.
/// </summary>
public class BehaviorPatternConverter
{
    /// <summary>
    /// Converts a <see cref="KernelBehaviorPattern"/> to a <see cref="BehaviorPattern"/>.
    /// </summary>
    /// <param name="pattern">The pattern to convert.</param>
    /// <returns>The pattern entity.</returns>
    public BehaviorPattern ToEntity(KernelBehaviorPattern pattern) => new()
    {
        GroupingKey = pattern.GroupingKey.Value,
        FacetSetHash = FacetSetHash.Of(pattern.Facets.Key),
        FacetSetKey = pattern.Facets.Key.Value,
        FacetsJson = JsonSerializer.Serialize(
            pattern.Facets.Facets.ToDictionary(facet => facet.Name.Value, facet => facet.Value.Value)),
        Occurrences = pattern.Occurrences.Value,
        Confidence = pattern.Confidence.Value,
        Support = pattern.Support.Value,
        Weight = pattern.Weight.Value,
        FirstSeen = pattern.FirstSeen,
        LastSeen = pattern.LastSeen
    };

    /// <summary>
    /// Converts a <see cref="BehaviorPattern"/> to a <see cref="KernelBehaviorPattern"/>.
    /// </summary>
    /// <param name="entity">The pattern entity.</param>
    /// <returns>The kernel pattern.</returns>
    public KernelBehaviorPattern ToKernel(BehaviorPattern entity)
    {
        var facets = string.IsNullOrEmpty(entity.FacetsJson)
            ? []
            : JsonSerializer.Deserialize<Dictionary<string, string>>(entity.FacetsJson) ?? [];

        return new KernelBehaviorPattern(
            entity.GroupingKey,
            new FacetSet(facets.Select(pair => new Facet(new FacetName(pair.Key), new FacetValue(pair.Value)))),
            entity.Occurrences,
            entity.Confidence,
            entity.Support,
            entity.Weight,
            entity.FirstSeen,
            entity.LastSeen);
    }
}
