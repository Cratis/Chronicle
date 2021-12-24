// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Streams;

namespace Events.Store.Orleans.Streams
{
    public class EventLogQueueAdapterReceiver : IQueueAdapterReceiver
    {
        readonly QueueId _queueId;

        public EventLogQueueAdapterReceiver(QueueId queueId)
        {
            _queueId = queueId;
        }

        /// <inheritdoc/>
        public Task<IList<IBatchContainer>> GetQueueMessagesAsync(int maxCount)
        {
            return Task.FromResult<IList<IBatchContainer>>(new List<IBatchContainer>());
        }

        /// <inheritdoc/>
        public Task Initialize(TimeSpan timeout)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task MessagesDeliveredAsync(IList<IBatchContainer> messages)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task Shutdown(TimeSpan timeout)
        {
            return Task.CompletedTask;
        }
    }
}
