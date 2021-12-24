// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Orleans.Streams;

namespace Cratis.Events.Store.Orleans.StreamProvider
{
    public class EventLogQueueAdapter : IQueueAdapter
    {
        readonly ConcurrentDictionary<QueueId, EventLogQueueAdapterReceiver> _receivers = new();

        readonly IStreamQueueMapper _mapper;

        public EventLogQueueAdapter(string name, IStreamQueueMapper mapper)
        {
            Name = name;
            _mapper = mapper;
        }

        public string Name { get; }

        public bool IsRewindable => true;

        public StreamProviderDirection Direction => StreamProviderDirection.ReadWrite;

        public IQueueAdapterReceiver CreateReceiver(QueueId queueId)
        {
            var receiver = new EventLogQueueAdapterReceiver(queueId);
            _receivers[queueId] = receiver;
            return receiver;
        }

        public Task QueueMessageBatchAsync<T>(Guid streamGuid, string streamNamespace, IEnumerable<T> events, StreamSequenceToken token, Dictionary<string, object> requestContext)
        {
            var queueId = _mapper.GetQueueForStream(streamGuid, streamNamespace);
            _receivers[queueId].AddMessage((events as IEnumerable<object>)!);

            return Task.CompletedTask;
        }
    }
}
