// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Streams;

namespace Events.Store.Orleans.Streams
{
    public class EventLogQueueAdapter : IQueueAdapter
    {
        readonly IStreamQueueMapper _mapper;

        public EventLogQueueAdapter(string name, IStreamQueueMapper mapper)
        {
            Name = name;
            _mapper = mapper;
        }

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public bool IsRewindable => true;

        /// <inheritdoc/>
        public StreamProviderDirection Direction => StreamProviderDirection.ReadWrite;

        /// <inheritdoc/>
        public IQueueAdapterReceiver CreateReceiver(QueueId queueId) => new EventLogQueueAdapterReceiver(queueId);

        /// <inheritdoc/>
        public Task QueueMessageBatchAsync<T>(Guid streamGuid, string streamNamespace, IEnumerable<T> events, StreamSequenceToken token, Dictionary<string, object> requestContext)
        {
            var queue = _mapper.GetQueueForStream(streamGuid, streamNamespace);

            return Task.CompletedTask;
        }
    }
}
