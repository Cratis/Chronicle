// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store;

/// <summary>
/// Defines a storage provider for the event sequence.
/// </summary>
public interface IEventSequenceStorageProvider
{
    /// <summary>
    /// Get the sequence number of the first event as part of the filtered event types.
    /// </summary>
    /// <param name="eventSequenceId">The event sequence to get for.</param>
    /// <param name="eventTypes">Optional event types to get for.</param>
    /// <param name="eventSourceId">Optional <see cref="EventSourceId"/> to get for. It won't filter by this if omitted.</param>
    /// <returns>The last sequence number. If providing event types, this will give the last sequence number from the selection of event types.</returns>
    Task<EventSequenceNumber> GetHeadSequenceNumber(EventSequenceId eventSequenceId, IEnumerable<EventType>? eventTypes = null, EventSourceId? eventSourceId = null);

    /// <summary>
    /// Get the sequence number of the last event as part of the filtered event types.
    /// </summary>
    /// <param name="eventSequenceId">The event sequence to get for.</param>
    /// <param name="eventTypes">Optional event types to get for.</param>
    /// <param name="eventSourceId">Optional <see cref="EventSourceId"/> to get for. It won't filter by this if omitted.</param>
    /// <returns>The last sequence number. If providing event types, this will give the last sequence number from the selection of event types.</returns>
    Task<EventSequenceNumber> GetTailSequenceNumber(EventSequenceId eventSequenceId, IEnumerable<EventType>? eventTypes = null, EventSourceId? eventSourceId = null);

    /// <summary>
    /// Get the last instance of a specific event type for an event source.
    /// </summary>
    /// <param name="eventSequenceId">The event sequence to get for.</param>
    /// <param name="eventTypeId"><see cref="EventTypeId"/> to get for.</param>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to get for.</param>
    /// <returns>The <see cref="AppendedEvent"/> found.</returns>
    Task<AppendedEvent> GetLastInstanceFor(EventSequenceId eventSequenceId, EventTypeId eventTypeId, EventSourceId eventSourceId);

    /// <summary>
    /// Get events using a specific sequence number as starting point within the event sequence.
    /// </summary>
    /// <param name="eventSequenceId">The event sequence to get for.</param>
    /// <param name="sequenceNumber">The <see cref="EventSequenceNumber"/> to get from.</param>
    /// <param name="eventSourceId">Optional <see cref="EventSourceId"/> to filter for.</param>
    /// <param name="eventTypes">Optional collection of <see cref="EventType">event types</see> to filter for.</param>
    /// <returns><see cref="IEventCursor"/>.</returns>
    Task<IEventCursor> GetFromSequenceNumber(EventSequenceId eventSequenceId, EventSequenceNumber sequenceNumber, EventSourceId? eventSourceId = default, IEnumerable<EventType>? eventTypes = default);

    /// <summary>
    /// Get events within a specific sequence number range.
    /// </summary>
    /// <param name="eventSequenceId">The event sequence to get for.</param>
    /// <param name="start">Start of the range.</param>
    /// <param name="end">End of the range.</param>
    /// <param name="eventSourceId">Optional <see cref="EventSourceId"/> to filter for.</param>
    /// <param name="eventTypes">Optional collection of <see cref="EventType">event types</see> to filter for.</param>
    /// <returns><see cref="IEventCursor"/>.</returns>
    Task<IEventCursor> GetRange(EventSequenceId eventSequenceId, EventSequenceNumber start, EventSequenceNumber end, EventSourceId? eventSourceId = default, IEnumerable<EventType>? eventTypes = default);
}
