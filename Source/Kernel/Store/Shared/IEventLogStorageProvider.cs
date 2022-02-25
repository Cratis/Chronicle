// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store;

/// <summary>
/// Defines a storage provider for the event sequence.
/// </summary>
public interface IEventLogStorageProvider
{
    /// <summary>
    /// Get the last instance of a specific event type for an event source.
    /// </summary>
    /// <param name="eventTypeId"><see cref="EventTypeId"/> to get for.</param>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to get for.</param>
    /// <returns>The <see cref="AppendedEvent"/> found.</returns>
    Task<AppendedEvent> GetLastInstanceFor(EventTypeId eventTypeId, EventSourceId eventSourceId);

    /// <summary>
    /// Get events using a specific sequence number as starting point within the event sequence.
    /// </summary>
    /// <param name="sequenceNumber">The <see cref="EventSequenceNumber"/> to get from.</param>
    /// <param name="eventSourceId">Optional <see cref="EventSourceId"/> to filter for.</param>
    /// <param name="eventTypes">Optional collection of <see cref="EventType">event types</see> to filter for.</param>
    /// <returns><see cref="IEventCursor"/>.</returns>
    Task<IEventCursor> GetFromSequenceNumber(EventSequenceNumber sequenceNumber, EventSourceId? eventSourceId = default, IEnumerable<EventType>? eventTypes = default);
}
