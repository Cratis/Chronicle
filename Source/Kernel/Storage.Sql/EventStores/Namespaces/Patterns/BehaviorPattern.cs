// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Patterns;

/// <summary>
/// Represents a behavior pattern that survived mining, as stored in SQL.
/// </summary>
/// <remarks>
/// The row is keyed by the scope and a fixed-width hash of the facet set rather than by the facet set key itself.
/// The key is a readable, unbounded string - fine as a value, but a primary key made of it would grow past the
/// index key size limits of the supported providers as soon as a pattern combined a few long-named facets.
/// </remarks>
[Table("BehaviorPatterns")]
public record BehaviorPattern
{
    /// <summary>
    /// Gets the scope the pattern belongs to.
    /// </summary>
    [StringLength(200)]
    public string GroupingKey { get; init; } = string.Empty;

    /// <summary>
    /// Gets the fixed-width hash of <see cref="FacetSetKey"/> the row is keyed by.
    /// </summary>
    [StringLength(64)]
    public string FacetSetHash { get; init; } = string.Empty;

    /// <summary>
    /// Gets the canonical key of the facet set the pattern is expressed in.
    /// </summary>
    public string FacetSetKey { get; init; } = string.Empty;

    /// <summary>
    /// Gets the JSON representation of the facets, keyed by facet name.
    /// </summary>
    public string FacetsJson { get; init; } = string.Empty;

    /// <summary>
    /// Gets how many times the pattern has been observed.
    /// </summary>
    public long Occurrences { get; init; }

    /// <summary>
    /// Gets how often the pattern holds when its context is present.
    /// </summary>
    public double Confidence { get; init; }

    /// <summary>
    /// Gets the share of all observed events the pattern was seen in.
    /// </summary>
    public double Support { get; init; }

    /// <summary>
    /// Gets the recency-weighted strength of the pattern.
    /// </summary>
    public double Weight { get; init; }

    /// <summary>
    /// Gets when the pattern was first observed.
    /// </summary>
    public DateTimeOffset FirstSeen { get; init; }

    /// <summary>
    /// Gets when the pattern was last observed.
    /// </summary>
    public DateTimeOffset LastSeen { get; init; }
}
