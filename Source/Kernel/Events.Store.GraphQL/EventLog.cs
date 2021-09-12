// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.GraphQL;

namespace Cratis.Events.Store.GraphQL
{
    /// <summary>
    /// Represents the API surface for working with event logs.
    /// </summary>
    [GraphRoot("events/store/log")]
    public class EventLog : GraphController
    {
        readonly IEventStore _eventStore;

        public EventLog(IEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        /// <summary>
        /// Commit an event to a specific event log.
        /// </summary>
        /// <param name="eventLog"><see cref="EventLogId"/> to commit to.</param>
        /// <param name="eventSourceId"><see cref="EventSourceId"/> to commit for.</param>
        /// <param name="eventType"><see cref="EventType"/> to commit.</param>
        /// <param name="content">JSON payload for the event.</param>
        /// <returns>True if successful, false if not.</returns>
        [Mutation]
        public async Task<bool> Commit(EventLogId eventLog, EventSourceId eventSourceId, EventType eventType, string content)
        {
            await _eventStore.GetEventLog(eventLog).Commit(eventSourceId, eventType, content);
            return true;
        }
    }
}
