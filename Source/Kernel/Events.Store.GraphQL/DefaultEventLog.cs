// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.GraphQL;

namespace Cratis.Events.Store.GraphQL
{
    [GraphRoot("events/store/log/default")]
    public class DefaultEventLog : GraphController
    {
        readonly IEventStore _eventStore;

        public DefaultEventLog(IEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        [Mutation]
        public async Task<bool> Commit(EventSourceId eventSourceId, EventType eventType, string content)
        {
            await _eventStore.DefaultEventLog.Commit(eventSourceId, eventType, content);
            return true;
        }
    }
}
