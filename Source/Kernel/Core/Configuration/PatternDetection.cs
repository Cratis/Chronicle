// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the configuration for pattern detection.
/// </summary>
public class PatternDetection
{
    /// <summary>
    /// Gets the facets that take part in the mined itemset key.
    /// </summary>
    /// <remarks>
    /// <see cref="FacetName.Year"/> and <see cref="FacetName.Month"/> are deliberately absent: they are kept on a
    /// surviving pattern for recency, but combining them multiplies the candidate space by every month the store
    /// has been running while splitting one behavior across all of them. Add them here when a deployment wants to
    /// mine seasonality and can afford the cardinality.
    /// </remarks>
    public IReadOnlyList<string> Facets { get; init; } =
    [
        FacetName.CommandType.Value,
        FacetName.InitiatorType.Value,
        FacetName.CausedByCommand.Value,
        FacetName.AggregateType.Value,
        FacetName.Day.Value,
        FacetName.TimeBucket.Value
    ];

    /// <summary>
    /// Gets the largest number of facets a mined itemset may combine.
    /// </summary>
    /// <remarks>
    /// The candidate space grows as the binomial coefficient of the facet count over this, so it is what keeps
    /// mining polynomial rather than exponential in the number of facets.
    /// </remarks>
    public int MaximumCombinationSize { get; init; } = 3;

    /// <summary>
    /// Gets the Lossy Counting error parameter, bounding how far a counted frequency may lag the true one.
    /// </summary>
    /// <remarks>
    /// Memory is bounded at roughly the logarithm of the observed count over this value, so a smaller value buys
    /// accuracy with memory. It must be greater than zero and smaller than <see cref="MinimumSupport"/> for the
    /// guarantee to mean anything.
    /// </remarks>
    public double Error { get; init; } = 0.001;

    /// <summary>
    /// Gets the smallest share of observed events an itemset must hold to survive as a pattern.
    /// </summary>
    public double MinimumSupport { get; init; } = 0.01;

    /// <summary>
    /// Gets the smallest confidence an itemset must hold to survive as a pattern.
    /// </summary>
    public double MinimumConfidence { get; init; } = 0.5;

    /// <summary>
    /// Gets the daily decay applied to the weight of an itemset that has gone unseen.
    /// </summary>
    /// <remarks>
    /// Applied as weight multiplied by this raised to the number of days since the itemset was last seen, so a
    /// value of 1 disables decay entirely and a smaller value forgets faster.
    /// </remarks>
    public double DecayFactor { get; init; } = 0.99;
}
