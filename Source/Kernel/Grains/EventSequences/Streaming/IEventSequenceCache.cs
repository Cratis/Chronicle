// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;

namespace Cratis.Chronicle.Grains.EventSequences.Streaming;

/// <summary>
/// Defines a system that can cache events.
/// </summary>
public interface IEventSequenceCache : IDisposable
{
    /// <summary>
    /// Gets the number of events in the cache.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets the head sequence numbers.
    /// </summary>
    EventSequenceNumber Head { get; }

    /// <summary>
    /// Gets the tail sequence numbers.
    /// </summary>
    EventSequenceNumber Tail { get; }

    /// <summary>
    /// Add an event to the cache.
    /// </summary>
    /// <param name="event"><see cref="AppendedEvent"/> to add.</param>
    void Add(AppendedEvent @event);

    /// <summary>
    /// Check if cache has a specific event based on its sequence number.
    /// </summary>
    /// <param name="sequenceNumber">Sequence number to check for.</param>
    /// <returns>True if it has the event, false if not.</returns>
    bool HasEvent(EventSequenceNumber sequenceNumber);

    /// <summary>
    /// Get an event from the cache based on its sequence number.
    /// </summary>
    /// <param name="sequenceNumber">Sequence number to get for.</param>
    /// <returns>The node representing the event.</returns>
    CachedAppendedEvent? GetEvent(EventSequenceNumber sequenceNumber);

    /// <summary>
    /// Populate the cache from a specific sequence number.
    /// </summary>
    /// <param name="from">The sequence number to populate from.</param>
    void Prime(EventSequenceNumber from);

    /// <summary>
    /// Prime the cache with the tail window.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task PrimeWithTailWindow();

    /// <summary>
    /// Check if the cache is under pressure.
    /// </summary>
    /// <returns>True if it is, false if not.</returns>
    bool IsUnderPressure();

    /// <summary>
    /// Purge items from cache if cache is under pressure.
    /// </summary>
    void Purge();
}
