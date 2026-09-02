// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Converts behavior patterns into the read model the pattern queries answer with.
/// </summary>
public static class PatternDetailsConverters
{
    /// <summary>
    /// Converts behavior patterns to their read model representation.
    /// </summary>
    /// <param name="patterns">The patterns to convert.</param>
    /// <returns>The converted patterns.</returns>
    public static IEnumerable<BehaviorPatternDetails> ToDetails(this IEnumerable<BehaviorPattern> patterns) =>
        [.. patterns.Select(pattern => pattern.ToDetails())];

    /// <summary>
    /// Converts a behavior pattern to its read model representation.
    /// </summary>
    /// <param name="pattern">The pattern to convert.</param>
    /// <returns>The converted pattern.</returns>
    /// <remarks>
    /// The identity is the scope followed by the facets rendered back into their canonical key. A pattern has no
    /// identifier of its own - it is the combination it constrains - and a view needs something stable to key a row
    /// on. The scope has to be part of it: the same combination is established independently by many scopes, so the
    /// facet key alone repeats once a view looks at more than one of them at a time.
    /// </remarks>
    public static BehaviorPatternDetails ToDetails(this BehaviorPattern pattern)
    {
        var facets = pattern.Facets.Facets.ToDictionary(facet => facet.Name.Value, facet => facet.Value.Value);

        return new(
            $"{pattern.GroupingKey}/{string.Join(';', facets.OrderBy(facet => facet.Key, StringComparer.Ordinal).Select(facet => $"{facet.Key}={facet.Value}"))}",
            pattern.GroupingKey,
            facets,
            pattern.Confidence,
            pattern.Support,
            pattern.Occurrences,
            pattern.Weight,
            pattern.Specificity,
            pattern.FirstSeen,
            pattern.LastSeen);
    }

    /// <summary>
    /// Converts a facet map to a <see cref="FacetSet"/>.
    /// </summary>
    /// <param name="facets">The facets to convert.</param>
    /// <returns>The converted <see cref="FacetSet"/>.</returns>
    public static FacetSet ToFacetSet(this IEnumerable<KeyValuePair<string, string>> facets) =>
        new(facets.Select(pair => new Facet(new FacetName(pair.Key), new FacetValue(pair.Value))));
}
