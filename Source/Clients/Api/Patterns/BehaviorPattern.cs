// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using IPatternsService = Cratis.Chronicle.Contracts.Patterns.IPatterns;

namespace Cratis.Chronicle.Api.Patterns;

/// <summary>
/// Represents a recurring combination of facets mined from an event store's history.
/// </summary>
/// <param name="Id">The identity of the pattern within its scope - the canonical facet set key.</param>
/// <param name="GroupingKey">The scope the pattern belongs to.</param>
/// <param name="Facets">The facets the pattern constrains, keyed by facet name.</param>
/// <param name="Confidence">How often the pattern holds when its context is present, in the range 0 to 1.</param>
/// <param name="Support">The share of all observed events the pattern was seen in, in the range 0 to 1.</param>
/// <param name="Occurrences">How many times the pattern has been observed.</param>
/// <param name="Weight">The recency-weighted strength of the pattern.</param>
/// <param name="Specificity">How many facets the pattern constrains.</param>
/// <param name="FirstSeen">When the pattern was first observed.</param>
/// <param name="LastSeen">When the pattern was last observed.</param>
[ReadModel]
public record BehaviorPattern(
    string Id,
    string GroupingKey,
    IDictionary<string, string> Facets,
    double Confidence,
    double Support,
    long Occurrences,
    double Weight,
    int Specificity,
    DateTimeOffset FirstSeen,
    DateTimeOffset LastSeen)
{
    /// <summary>
    /// Get every pattern a scope has established.
    /// </summary>
    /// <param name="patterns">The <see cref="IPatternsService"/> contract.</param>
    /// <param name="eventStore">The event store to get patterns for.</param>
    /// <param name="namespace">The namespace to get patterns for.</param>
    /// <param name="groupingKey">The scope to get patterns for.</param>
    /// <returns>Collection of <see cref="BehaviorPattern"/>.</returns>
    public static async Task<IEnumerable<BehaviorPattern>> PatternsForScope(
        IPatternsService patterns,
        string eventStore,
        string @namespace,
        string groupingKey) =>
        (await patterns.GetPatternsForScope(new()
        {
            EventStore = eventStore,
            Namespace = @namespace,
            GroupingKey = groupingKey
        })).ToApi();
}
