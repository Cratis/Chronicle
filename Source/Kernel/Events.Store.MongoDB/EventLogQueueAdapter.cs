// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.DependencyInversion;
using Orleans.Streams;

namespace Cratis.Events.Store.MongoDB
{
    public class EventLogQueueAdapter : IQueueAdapter
    {
        readonly ConcurrentDictionary<QueueId, EventLogQueueAdapterReceiver> _receivers = new();

        readonly IStreamQueueMapper _mapper;
        readonly ProviderFor<IEventLogs> _eventLogsProvider;

        public EventLogQueueAdapter(string name, IStreamQueueMapper mapper, ProviderFor<IEventLogs> eventLogsProvider)
        {
            Name = name;
            _mapper = mapper;
            _eventLogsProvider = eventLogsProvider;
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

        public async Task QueueMessageBatchAsync<T>(Guid streamGuid, string streamNamespace, IEnumerable<T> events, StreamSequenceToken token, Dictionary<string, object> requestContext)
        {
            foreach (var @event in events)
            {
                var appendedEvent = (@event as AppendedEvent)!;
                await _eventLogsProvider().Append(streamGuid, new((ulong)token.SequenceNumber), appendedEvent.EventContext.EventSourceId, appendedEvent.Metadata.EventType, appendedEvent.Content);
            }

            var queueId = _mapper.GetQueueForStream(streamGuid, streamNamespace);
            _receivers[queueId].AddMessage(token, (events as IEnumerable<object>)!, requestContext);
        }
    }
}
