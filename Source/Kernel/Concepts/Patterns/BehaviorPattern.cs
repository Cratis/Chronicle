// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Patterns;

/// <summary>
/// Represents a recurring combination of facets that survived the support and confidence thresholds.
/// </summary>
/// <param name="GroupingKey">The scope the pattern belongs to.</param>
/// <param name="Facets">The <see cref="FacetSet"/> the pattern is expressed in.</param>
/// <param name="Occurrences">How many times the pattern has been observed.</param>
/// <param name="Confidence">How often the pattern holds when its context is present.</param>
/// <param name="Support">The share of all observed events the pattern was seen in.</param>
/// <param name="Weight">The recency-weighted strength of the pattern.</param>
/// <param name="FirstSeen">When the pattern was first observed.</param>
/// <param name="LastSeen">When the pattern was last observed.</param>
/// <remarks>
/// Only surviving patterns are persisted, so storage scales with distinct recurring behavior rather than with event
/// volume - a store that appends millions of events but sees a few hundred recurring behaviors holds a few hundred
/// rows here.
/// </remarks>
public record BehaviorPattern(
    PatternGroupingKey GroupingKey,
    FacetSet Facets,
    PatternOccurrences Occurrences,
    PatternConfidence Confidence,
    PatternSupport Support,
    PatternWeight Weight,
    DateTimeOffset FirstSeen,
    DateTimeOffset LastSeen)
{
    /// <summary>
    /// Gets how specific the pattern is - the number of facets it constrains.
    /// </summary>
    public int Specificity => Facets.Specificity;

    /// <summary>
    /// Check whether the pattern applies to a context.
    /// </summary>
    /// <param name="context">The <see cref="FacetSet"/> describing the context, which may constrain more facets than the pattern does.</param>
    /// <returns>True when every facet the pattern constrains is present with the same value in the context, false when not.</returns>
    public bool Matches(FacetSet context) => Facets.IsSubsetOf(context);
}
