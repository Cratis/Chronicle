// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Store.GraphQL
{
    [GraphRoot("events/store/log/public")]
    public class PublicEventLog : GraphController
    {
        readonly IEventStore _eventStore;

        public PublicEventLog(IEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        [Mutation]
        public async Task<bool> Commit(EventSourceId eventSourceId, EventType eventType, string content)
        {
            await _eventStore.PublicEventLog.Commit(eventSourceId, eventType, content);
            return true;
        }
    }

}
