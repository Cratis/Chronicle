// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.DependencyInversion;
using Orleans.Streams;

namespace Cratis.Events.Store.MongoDB
{
    /// <summary>
    /// Represents an implementation of <see cref="IQueueAdapter"/> for MongoDB event log.
    /// </summary>
    public class EventLogQueueAdapter : IQueueAdapter
    {
        readonly ConcurrentDictionary<QueueId, EventLogQueueAdapterReceiver> _receivers = new();

        readonly IStreamQueueMapper _mapper;
        readonly ProviderFor<IEventLogs> _eventLogsProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogQueueAdapter"/> class.
        /// </summary>
        /// <param name="name">Name of stream.</param>
        /// <param name="mapper"></param>
        /// <param name="eventLogsProvider"></param>
        public EventLogQueueAdapter(
            string name,
            IStreamQueueMapper mapper,
            ProviderFor<IEventLogs> eventLogsProvider)
        {
            Name = name;
            _mapper = mapper;
            _eventLogsProvider = eventLogsProvider;
        }

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public bool IsRewindable => true;

        /// <inheritdoc/>
        public StreamProviderDirection Direction => StreamProviderDirection.ReadWrite;

        /// <inheritdoc/>
        public IQueueAdapterReceiver CreateReceiver(QueueId queueId)
        {
            var receiver = new EventLogQueueAdapterReceiver(queueId);
            _receivers[queueId] = receiver;
            return receiver;
        }

        /// <inheritdoc/>
        public async Task QueueMessageBatchAsync<T>(Guid streamGuid, string streamNamespace, IEnumerable<T> events, StreamSequenceToken token, Dictionary<string, object> requestContext)
        {
            var queueId = _mapper.GetQueueForStream(streamGuid, streamNamespace);
            if (token.SequenceNumber != -1)
            {
                foreach (var @event in events)
                {
                    var appendedEvent = (@event as AppendedEvent)!;
                    await _eventLogsProvider().Append(streamGuid, appendedEvent.Metadata.SequenceNumber, appendedEvent.EventContext.EventSourceId, appendedEvent.Metadata.EventType, appendedEvent.Content);
                }
            }

            _receivers[queueId].AddAppendedEvent(streamGuid, events.Cast<AppendedEvent>().ToArray(), requestContext);
        }
    }
}
