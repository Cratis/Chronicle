// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Represents what a <see cref="LossyCountingSketch"/> holds for one candidate itemset.
/// </summary>
/// <param name="Itemset">The <see cref="FacetSet"/> being counted.</param>
/// <param name="Frequency">How many times the itemset has been counted since it entered the sketch.</param>
/// <param name="Error">The largest number of occurrences that could have been missed before it entered.</param>
/// <param name="Weight">The recency-weighted strength of the itemset.</param>
/// <param name="FirstSeen">When the itemset was first counted.</param>
/// <param name="LastSeen">When the itemset was last counted.</param>
/// <remarks>
/// <see cref="Frequency"/> and <see cref="Error"/> are the Lossy Counting pair: the true count of an itemset in
/// the sketch is at least <see cref="Frequency"/> and at most <see cref="Frequency"/> plus <see cref="Error"/>.
/// An itemset that entered late carries a large error, which is what stops a newcomer from being mistaken for a
/// long-standing habit.
/// </remarks>
public record LossyCountingEntry(
    FacetSet Itemset,
    long Frequency,
    long Error,
    double Weight,
    DateTimeOffset FirstSeen,
    DateTimeOffset LastSeen)
{
    /// <summary>
    /// Gets the largest the true count of the itemset could be.
    /// </summary>
    public long MaximumFrequency => Frequency + Error;
}
