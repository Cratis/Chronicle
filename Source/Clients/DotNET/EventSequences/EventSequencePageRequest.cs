// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Requests a bounded, inclusive keyset page of an event sequence.
/// </summary>
/// <param name="From">The inclusive start, or null for the beginning (forward) or tail (backward).</param>
/// <param name="Count">The maximum events to return, between 1 and 1000.</param>
/// <param name="Direction">The read order.</param>
/// <param name="Until">The optional inclusive stop: an upper bound forward or lower bound backward.</param>
/// <param name="Filter">Optional filters applied before the page limit.</param>
public record EventSequencePageRequest(
    EventSequenceNumber? From,
    int Count,
    EventSequenceReadDirection Direction = EventSequenceReadDirection.Forward,
    EventSequenceNumber? Until = default,
    EventSequenceFilter? Filter = default)
{
    /// <summary>
    /// Creates an oldest-first page request.
    /// </summary>
    /// <param name="from">The inclusive start, or null for the beginning.</param>
    /// <param name="count">The maximum events, between 1 and 1000.</param>
    /// <param name="filter">Optional filters.</param>
    /// <returns>The forward page request.</returns>
    public static EventSequencePageRequest Forward(EventSequenceNumber? from, int count, EventSequenceFilter? filter = default) =>
        new(from, count, Filter: filter);

    /// <summary>
    /// Creates a newest-first page request.
    /// </summary>
    /// <param name="from">The inclusive start, or null for the tail.</param>
    /// <param name="count">The maximum events, between 1 and 1000.</param>
    /// <param name="filter">Optional filters.</param>
    /// <returns>The backward page request.</returns>
    public static EventSequencePageRequest Backward(EventSequenceNumber? from, int count, EventSequenceFilter? filter = default) =>
        new(from, count, EventSequenceReadDirection.Backward, Filter: filter);
}
