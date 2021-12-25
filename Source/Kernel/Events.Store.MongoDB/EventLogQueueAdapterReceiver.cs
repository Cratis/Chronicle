// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Streams;

namespace Cratis.Events.Store.MongoDB
{
    public class EventLogQueueAdapterReceiver : IQueueAdapterReceiver
    {
        readonly QueueId _queueId;
        readonly List<IBatchContainer> _events = new();

        public EventLogQueueAdapterReceiver(QueueId queueId)
        {
            _queueId = queueId;
        }

        public Task<IList<IBatchContainer>> GetQueueMessagesAsync(int maxCount)
        {
            var result = _events.ToArray().ToList();
            _events.Clear();
            return Task.FromResult<IList<IBatchContainer>>(result);
        }

        public Task Initialize(TimeSpan timeout)
        {
            return Task.CompletedTask;
        }

        public Task MessagesDeliveredAsync(IList<IBatchContainer> messages)
        {
            return Task.CompletedTask;
        }

        public Task Shutdown(TimeSpan timeout)
        {
            return Task.CompletedTask;
        }

        public void AddMessage(StreamSequenceToken token, IEnumerable<object> events, IDictionary<string, object> requestContext)
        {
            _events.Add(new EventLogBatchContainer(token, Guid.Empty, events, requestContext));
        }
    }
}
