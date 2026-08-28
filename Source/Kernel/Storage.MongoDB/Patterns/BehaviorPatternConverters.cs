// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;
using KernelBehaviorPattern = Cratis.Chronicle.Concepts.Patterns.BehaviorPattern;

namespace Cratis.Chronicle.Storage.MongoDB.Patterns;

/// <summary>
/// Extension methods for converting patterns between their kernel and MongoDB representations.
/// </summary>
public static class BehaviorPatternConverters
{
    /// <summary>
    /// Convert a kernel <see cref="KernelBehaviorPattern"/> to its MongoDB representation.
    /// </summary>
    /// <param name="pattern">The pattern to convert.</param>
    /// <returns>The converted <see cref="BehaviorPattern"/>.</returns>
    public static BehaviorPattern ToMongoDB(this KernelBehaviorPattern pattern) => new()
    {
        Id = IdFor(pattern.GroupingKey, pattern.Facets.Key),
        GroupingKey = pattern.GroupingKey,
        FacetSetKey = pattern.Facets.Key,
        Facets = [.. pattern.Facets.Facets.Select(facet => new PatternFacet { Name = facet.Name, Value = facet.Value })],
        Occurrences = pattern.Occurrences.Value,
        Confidence = pattern.Confidence.Value,
        Support = pattern.Support.Value,
        Weight = pattern.Weight.Value,
        FirstSeen = pattern.FirstSeen,
        LastSeen = pattern.LastSeen
    };

    /// <summary>
    /// Convert a MongoDB <see cref="BehaviorPattern"/> to its kernel representation.
    /// </summary>
    /// <param name="pattern">The pattern to convert.</param>
    /// <returns>The converted <see cref="KernelBehaviorPattern"/>.</returns>
    public static KernelBehaviorPattern ToKernel(this BehaviorPattern pattern) => new(
        pattern.GroupingKey,
        new FacetSet(pattern.Facets.Select(facet => new Facet(facet.Name, facet.Value))),
        pattern.Occurrences,
        pattern.Confidence,
        pattern.Support,
        pattern.Weight,
        pattern.FirstSeen,
        pattern.LastSeen);

    /// <summary>
    /// Gets the identifier a pattern is stored under.
    /// </summary>
    /// <param name="groupingKey">The <see cref="PatternGroupingKey"/> the pattern belongs to.</param>
    /// <param name="facetSetKey">The <see cref="FacetSetKey"/> identifying the facet combination.</param>
    /// <returns>The identifier.</returns>
    /// <remarks>
    /// A pattern is one facet combination within one scope, so the identity is the pair. Making that the document
    /// identifier is what lets a mining flush be an upsert rather than a read-compare-write.
    /// </remarks>
    public static string IdFor(PatternGroupingKey groupingKey, FacetSetKey facetSetKey) =>
        $"{groupingKey.Value}|{facetSetKey.Value}";
}
