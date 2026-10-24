// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Holds one bounded parent history and its predecessor, including an empty result for a missing source.
/// </summary>
/// <param name="from">The page's inclusive lower boundary.</param>
/// <param name="upTo">The page's inclusive upper boundary.</param>
/// <param name="events">The matching events and latest predecessor, in sequence order.</param>
internal sealed class ParentHistoryWindow(EventSequenceNumber from, EventSequenceNumber upTo, IReadOnlyList<AppendedEvent> events)
{
    /// <summary>
    /// Gets whether the window can answer a bounded lookup without fetching an older predecessor.
    /// </summary>
    /// <param name="start">The lookup's lower boundary.</param>
    /// <param name="end">The lookup's inclusive upper boundary.</param>
    /// <returns>Whether the requested history is covered.</returns>
    internal bool Covers(EventSequenceNumber start, EventSequenceNumber end) =>
        start >= from && start <= upTo && end <= upTo && (end >= from || (from > EventSequenceNumber.First && end == from - 1));

    /// <summary>
    /// Selects the latest eligible predecessor and the requested inclusive range.
    /// </summary>
    /// <param name="start">The inclusive lower boundary.</param>
    /// <param name="end">The inclusive upper boundary, also applied to the predecessor.</param>
    /// <returns>The bounded events in sequence order.</returns>
    internal IReadOnlyList<AppendedEvent> Select(EventSequenceNumber start, EventSequenceNumber end)
    {
        var predecessor = events.LastOrDefault(@event => @event.Context.SequenceNumber < start && @event.Context.SequenceNumber <= end);
        return events.Where(@event => ReferenceEquals(@event, predecessor) || (@event.Context.SequenceNumber >= start && @event.Context.SequenceNumber <= end)).ToArray();
    }

    /// <summary>
    /// Finds the latest parent eligible at the original child's boundary.
    /// </summary>
    /// <param name="sequenceNumber">The original child's sequence or deferred release boundary.</param>
    /// <param name="inclusive">Whether the releasing parent itself is eligible.</param>
    /// <returns>The eligible parent, or null when no parent is present.</returns>
    internal AppendedEvent? Find(EventSequenceNumber sequenceNumber, bool inclusive) =>
        events.LastOrDefault(@event => inclusive ? @event.Context.SequenceNumber <= sequenceNumber : @event.Context.SequenceNumber < sequenceNumber);
}
