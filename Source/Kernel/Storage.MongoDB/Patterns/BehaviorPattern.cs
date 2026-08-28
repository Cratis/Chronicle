// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Storage.MongoDB.Patterns;

/// <summary>
/// Represents the MongoDB representation of a <see cref="Concepts.Patterns.BehaviorPattern"/>.
/// </summary>
/// <remarks>
/// The facet set is stored as its canonical key alongside the facets it is made of. The key is what lookups filter
/// on, and the facets are what a caller reads - deriving one from the other on every read would make the lookup a
/// scan, and storing only the key would make the pattern unreadable.
/// </remarks>
public class BehaviorPattern
{
    /// <summary>
    /// Gets or sets the identifier of the pattern, unique across scopes.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the <see cref="PatternGroupingKey"/> the pattern belongs to.
    /// </summary>
    public PatternGroupingKey GroupingKey { get; set; } = PatternGroupingKey.Unspecified;

    /// <summary>
    /// Gets or sets the <see cref="FacetSetKey"/> identifying the facet combination.
    /// </summary>
    public FacetSetKey FacetSetKey { get; set; } = FacetSetKey.Empty;

    /// <summary>
    /// Gets or sets the facets the pattern is expressed in.
    /// </summary>
    public IList<PatternFacet> Facets { get; set; } = [];

    /// <summary>
    /// Gets or sets how many times the pattern has been observed.
    /// </summary>
    public long Occurrences { get; set; }

    /// <summary>
    /// Gets or sets how often the pattern holds when its context is present.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets the share of all observed events the pattern was seen in.
    /// </summary>
    public double Support { get; set; }

    /// <summary>
    /// Gets or sets the recency-weighted strength of the pattern.
    /// </summary>
    public double Weight { get; set; }

    /// <summary>
    /// Gets or sets when the pattern was first observed.
    /// </summary>
    public DateTimeOffset FirstSeen { get; set; }

    /// <summary>
    /// Gets or sets when the pattern was last observed.
    /// </summary>
    public DateTimeOffset LastSeen { get; set; }
}
