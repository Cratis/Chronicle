// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming;

/// <summary>
/// Defines a system that can cache events.
/// </summary>
public interface IEventSequenceCache : IDisposable
{
    /// <summary>
    /// Add an event to the cache.
    /// </summary>
    /// <param name="event"><see cref="AppendedEvent"/> to add.</param>
    void Add(AppendedEvent @event);

    /// <summary>
    /// Get a view of the cache.
    /// </summary>
    /// <param name="from">The from sequence number.</param>
    /// <param name="to">Optional to sequence number.</param>
    /// <returns>A view containing a collection of <see cref="AppendedEvent"/>.</returns>
    SortedSet<AppendedEvent> GetView(EventSequenceNumber from, EventSequenceNumber? to = null);

    /// <summary>
    /// Populate the cache from a specific sequence number.
    /// </summary>
    /// <param name="from">The sequence number to populate from.</param>
    void Prime(EventSequenceNumber from);

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
