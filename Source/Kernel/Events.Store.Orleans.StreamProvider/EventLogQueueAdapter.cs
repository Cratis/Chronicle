// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Streams;

namespace Events.Store.Orleans.Streams
{
    /// <summary>
    /// Represents a <see cref="IQueueAdapter"/> for event log.
    /// </summary>
    public class EventLogQueueAdapter : IQueueAdapter
    {
        public const string StreamName = "event-log";

        /// <inheritdoc/>
        public string Name => StreamName;

        /// <inheritdoc/>
        public bool IsRewindable => true;

        /// <inheritdoc/>
        public StreamProviderDirection Direction => StreamProviderDirection.ReadWrite;

        /// <inheritdoc/>
        public IQueueAdapterReceiver CreateReceiver(QueueId queueId) => new EventLogQueueAdapterReceiver();

        /// <inheritdoc/>
        public Task QueueMessageBatchAsync<T>(Guid streamGuid, string streamNamespace, IEnumerable<T> events, StreamSequenceToken token, Dictionary<string, object> requestContext)
        {
            return Task.CompletedTask;
        }
    }
}
