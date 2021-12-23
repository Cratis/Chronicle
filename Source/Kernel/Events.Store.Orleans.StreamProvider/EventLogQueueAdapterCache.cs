// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Streams;

namespace Events.Store.Orleans.Streams
{
    public class EventLogQueueAdapterCache : IQueueAdapterCache
    {
        public IQueueCache CreateQueueCache(QueueId queueId) => new EventLogQueueCache();
    }
}
