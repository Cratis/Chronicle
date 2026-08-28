// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;
using Contract = Cratis.Chronicle.Contracts.Patterns;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Extension methods for converting patterns from their contract representation.
/// </summary>
public static class PatternConverters
{
    /// <summary>
    /// Convert a collection of contract patterns to their client representation.
    /// </summary>
    /// <param name="patterns">The patterns to convert.</param>
    /// <returns>The converted <see cref="BehaviorPattern">patterns</see>.</returns>
    public static IEnumerable<BehaviorPattern> ToClient(this IEnumerable<Contract.Pattern> patterns) =>
        [.. patterns.Select(pattern => pattern.ToClient())];

    /// <summary>
    /// Convert a contract pattern to its client representation.
    /// </summary>
    /// <param name="pattern">The pattern to convert.</param>
    /// <returns>The converted <see cref="BehaviorPattern"/>.</returns>
    public static BehaviorPattern ToClient(this Contract.Pattern pattern) => new(
        pattern.GroupingKey,
        new FacetSet(pattern.Facets.Select(facet => new Facet(new FacetName(facet.Key), new FacetValue(facet.Value)))),
        pattern.Occurrences,
        pattern.Confidence,
        pattern.Support,
        pattern.Weight,
        pattern.FirstSeen,
        pattern.LastSeen);
}
