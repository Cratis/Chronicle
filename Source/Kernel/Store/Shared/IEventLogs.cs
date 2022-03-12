// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Aksio.Cratis.Events.Store
{
    /// <summary>
    /// Defines an immutable event log.
    /// </summary>
    public interface IEventLogs
    {
        /// <summary>
        /// Append a single event to the event store.
        /// </summary>
        /// <param name="eventLogId">The <see cref="EventLogId"/> representing the event log to append to.</param>
        /// <param name="sequenceNumber">The unique <see cref="EventLogSequenceNumber">sequence number</see> within the event log.</param>
        /// <param name="eventSourceId">The <see cref="EventSourceId"/> to append for.</param>
        /// <param name="eventType">The <see cref="EventType">type of event</see> to append.</param>
        /// <param name="content">The JSON payload of the event.</param>
        /// <returns>Awaitable <see cref="Task"/>.</returns>
        Task Append(EventLogId eventLogId, EventLogSequenceNumber sequenceNumber, EventSourceId eventSourceId, EventType eventType, JsonObject content);

        /// <summary>
        /// Compensate a single event to the event store.
        /// </summary>
        /// <param name="eventLogId">The <see cref="EventLogId"/> representing the event log to append to.</param>
        /// <param name="sequenceNumber">The unique <see cref="EventLogSequenceNumber">sequence number</see> within the event log.</param>
        /// <param name="eventType">The <see cref="EventType">type of event</see> to append.</param>
        /// <param name="content">The JSON payload of the event.</param>
        /// <returns>Awaitable <see cref="Task"/>.</returns>
        Task Compensate(EventLogId eventLogId, EventLogSequenceNumber sequenceNumber, EventType eventType, JsonObject content);

        /// <summary>
        /// Find <see cref="AppendedEvent">appended events</see> from the event log.
        /// </summary>
        /// <param name="eventLogId">The <see cref="EventLogId"/> representing the event log to find from.</param>
        /// <param name="eventSourceId">Optional <see cref="EventSourceId"/> to find for.</param>
        /// <returns>Awaitable <see cref="Task"/> with <see cref="IEventCursor"/>.</returns>
        /// <remarks>
        /// If no <see cref="EventSourceId"/> has been specified, it will find for all event sources.
        /// </remarks>
        Task<IEventCursor> FindFor(EventLogId eventLogId, EventSourceId? eventSourceId = default);
    }
}
