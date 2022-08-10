// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.EventSequences.Caching;

/// <summary>
/// Defines a cache of appended events for an event sequence.
/// </summary>
/// <remarks>
/// All ranges are inclusive on both start and end sequence number.
/// </remarks>
public interface IEventSequenceCache
{
    /// <summary>
    /// Gets the <see cref="EventSequenceId"/> the cache is for.
    /// </summary>
    EventSequenceId EventSequenceId { get; }

    /// <summary>
    /// Gets the range size kept in cache.
    /// </summary>
    int RangeSize { get; }

    /// <summary>
    /// Gets the current <see cref="EventSequenceCacheRange"/>.
    /// </summary>
    EventSequenceCacheRange CurrentRange { get; }

    /// <summary>
    /// Gets the cache content.
    /// </summary>
    IEnumerable<AppendedEvent> Content { get; }

    /// <summary>
    /// Feed the cache with events.
    /// </summary>
    /// <param name="events">Collection of <see cref="AppendedEvent"/>.</param>
    void Feed(IEnumerable<AppendedEvent> events);

    /// <summary>
    /// Get all events from a specific position.
    /// </summary>
    /// <param name="sequenceNumber">Start <see cref="EventSequenceNumber"/>.</param>
    /// <returns><see cref="IEventCursor"/>.</returns>
    /// <remarks>
    /// The cache manages itself. This means that it will populate what it needs to fulfill its promise.
    /// </remarks>
    IEventCursor GetFrom(EventSequenceNumber sequenceNumber);

    /// <summary>
    /// Get a range of events.
    /// </summary>
    /// <param name="start">Start <see cref="EventSequenceNumber"/>.</param>
    /// <param name="end">End <see cref="EventSequenceNumber"/>.</param>
    /// <returns><see cref="IEventCursor"/>.</returns>
    /// <remarks>
    /// The cache manages itself. This means that it will populate what it needs to fulfill its promise.
    /// </remarks>
    IEventCursor GetRange(EventSequenceNumber start, EventSequenceNumber end);
}
