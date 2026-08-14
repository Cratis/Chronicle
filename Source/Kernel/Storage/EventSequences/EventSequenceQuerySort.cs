// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// Represents how an event sequence query is ordered.
/// </summary>
/// <param name="By">What the events are ordered by.</param>
/// <param name="Descending">Whether the order runs from the highest value down rather than from the lowest up.</param>
public record EventSequenceQuerySort(
    EventSequenceQuerySortBy By = EventSequenceQuerySortBy.SequenceNumber,
    bool Descending = false)
{
    /// <summary>
    /// Gets the order a query takes when the caller does not ask for one - oldest first, by position
    /// in the sequence.
    /// </summary>
    public static readonly EventSequenceQuerySort Default = new();

    /// <summary>
    /// Gets the order that puts the newest event in the sequence first.
    /// </summary>
    public static readonly EventSequenceQuerySort NewestFirst = new(Descending: true);
}
