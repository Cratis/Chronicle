// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Extensions.Orleans.Execution;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.MongoDB
{
    /// <summary>
    /// Represents an implementation of <see cref="EventLogQueueAdapterReceiver"/> for MongoDB event log.
    /// </summary>
    public class EventLogQueueAdapterReceiver : IQueueAdapterReceiver
    {
        readonly ConcurrentBag<IBatchContainer> _eventBatches = new();

        /// <inheritdoc/>
        public Task<IList<IBatchContainer>> GetQueueMessagesAsync(int maxCount)
        {
            lock (_eventBatches)
            {
                var result = _eventBatches.OrderBy(_ => _.SequenceToken).ToArray().ToList();
                _eventBatches.Clear();
                return Task.FromResult<IList<IBatchContainer>>(result);
            }
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

        /// <summary>
        /// Add an appended event to the receivers queue.
        /// </summary>
        /// <param name="streamGuid">The <see cref="Guid"/> identifying the stream.</param>
        /// <param name="events"><see cref="AppendedEvent">Events</see> to add.</param>
        /// <param name="requestContext">The request context.</param>
        public void AddAppendedEvent(Guid streamGuid, IEnumerable<AppendedEvent> events, IDictionary<string, object> requestContext)
        {
            lock (_eventBatches)
            {
                var tenantIdAsString = requestContext[RequestContextKeys.TenantId]?.ToString() ?? TenantId.NotSet.ToString();
                var tenantId = (TenantId)tenantIdAsString;

                _eventBatches.Add(new EventLogBatchContainer(events, streamGuid, tenantId, requestContext));
            }
        }
    }
}
