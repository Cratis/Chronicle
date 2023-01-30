// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Kernel.EventSequences;

/// <summary>
/// Defines a system that can cache events.
/// </summary>
public interface IEventSequenceCache
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
    IEnumerable<AppendedEvent> GetView(EventSequenceNumber from, EventSequenceNumber? to = null);
}
