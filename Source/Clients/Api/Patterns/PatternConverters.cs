// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Contracts.Patterns;

namespace Cratis.Chronicle.Api.Patterns;

/// <summary>
/// Extension methods for converting patterns to their API representation.
/// </summary>
public static class PatternConverters
{
    /// <summary>
    /// Convert a collection of contract patterns to their API representation.
    /// </summary>
    /// <param name="patterns">The patterns to convert.</param>
    /// <returns>The converted <see cref="BehaviorPattern">patterns</see>.</returns>
    public static IEnumerable<BehaviorPattern> ToApi(this IEnumerable<Contract.Pattern> patterns) =>
        [.. patterns.Select(pattern => pattern.ToApi())];

    /// <summary>
    /// Convert a contract pattern to its API representation.
    /// </summary>
    /// <param name="pattern">The pattern to convert.</param>
    /// <returns>The converted <see cref="BehaviorPattern"/>.</returns>
    /// <remarks>
    /// The identity is the scope followed by the facets rendered back into their canonical key. A pattern has no
    /// identifier of its own - it is the combination it constrains - and a view needs something stable to key a row
    /// on. The scope has to be part of it: the same combination is established independently by many scopes, so the
    /// facet key alone repeats once a view looks at more than one of them at a time.
    /// </remarks>
    public static BehaviorPattern ToApi(this Contract.Pattern pattern) => new(
        $"{pattern.GroupingKey}/{string.Join(';', pattern.Facets.OrderBy(facet => facet.Key, StringComparer.Ordinal).Select(facet => $"{facet.Key}={facet.Value}"))}",
        pattern.GroupingKey,
        pattern.Facets,
        pattern.Confidence,
        pattern.Support,
        pattern.Occurrences,
        pattern.Weight,
        pattern.Facets.Count,
        pattern.FirstSeen ?? DateTimeOffset.MinValue,
        pattern.LastSeen ?? DateTimeOffset.MinValue);
}
