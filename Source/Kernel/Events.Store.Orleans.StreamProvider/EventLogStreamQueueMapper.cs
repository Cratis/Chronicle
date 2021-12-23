// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Streams;

namespace Events.Store.Orleans.Streams
{
    public class EventLogStreamQueueMapper : IStreamQueueMapper
    {
        public IEnumerable<QueueId> GetAllQueues() => throw new NotImplementedException();
        public QueueId GetQueueForStream(Guid streamGuid, string streamNamespace) => throw new NotImplementedException();
    }
}
