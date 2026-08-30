// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;
using Contract = Cratis.Chronicle.Contracts.Patterns;

namespace Cratis.Chronicle.Services.Patterns;

/// <summary>
/// Extension methods for converting patterns between their kernel and contract representations.
/// </summary>
public static class PatternConverters
{
    /// <summary>
    /// Convert a kernel <see cref="BehaviorPattern"/> to its contract representation.
    /// </summary>
    /// <param name="pattern">The pattern to convert.</param>
    /// <returns>The converted <see cref="Contract.Pattern"/>.</returns>
    public static Contract.Pattern ToContract(this BehaviorPattern pattern) => new()
    {
        GroupingKey = pattern.GroupingKey.Value,
        Facets = pattern.Facets.Facets.ToDictionary(facet => facet.Name.Value, facet => facet.Value.Value),
        Confidence = pattern.Confidence.Value,
        Support = pattern.Support.Value,
        Occurrences = pattern.Occurrences.Value,
        Weight = pattern.Weight.Value,
        FirstSeen = pattern.FirstSeen,
        LastSeen = pattern.LastSeen
    };

    /// <summary>
    /// Convert a contract facet map to a <see cref="FacetSet"/>.
    /// </summary>
    /// <param name="facets">The facets to convert.</param>
    /// <returns>The converted <see cref="FacetSet"/>.</returns>
    public static FacetSet ToFacetSet(this IEnumerable<KeyValuePair<string, string>> facets) =>
        new(facets.Select(pair => new Facet(new FacetName(pair.Key), new FacetValue(pair.Value))));
}
