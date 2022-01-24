// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store
{
    /// <summary>
    /// Defines a storage provider for the event log.
    /// </summary>
    public interface IEventLogStorageProvider
    {
        /// <summary>
        /// Get events using a specific sequence number as starting point within the event log.
        /// </summary>
        /// <param name="sequenceNumber">The <see cref="EventLogSequenceNumber"/> to get from.</param>
        /// <param name="eventSourceId">Optional <see cref="EventSourceId"/> to filter for.</param>
        /// <param name="eventTypes">Optional collection of <see cref="EventType">event types</see> to filter for.</param>
        /// <returns><see cref="IEventCursor"/>.</returns>
        Task<IEventCursor> GetFromSequenceNumber(EventLogSequenceNumber sequenceNumber, EventSourceId? eventSourceId = default, IEnumerable<EventType>? eventTypes = default);
    }
}
