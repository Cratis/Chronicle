// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Aksio.Cratis.Execution;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="EventSequenceQueueAdapterReceiver"/> for MongoDB event log.
/// </summary>
public class EventSequenceQueueAdapterReceiver : IQueueAdapterReceiver
{
    readonly ConcurrentBag<IBatchContainer> _eventBatches = new();
    readonly List<IBatchContainer> _empty = new();

    /// <inheritdoc/>
    public Task<IList<IBatchContainer>> GetQueueMessagesAsync(int maxCount)
    {
        lock (_eventBatches)
        {
            if (!_eventBatches.IsEmpty)
            {
                var result = _eventBatches.OrderBy(_ => _.SequenceToken).ToArray().ToList();
                _eventBatches.Clear();
                return Task.FromResult<IList<IBatchContainer>>(result);
            }
        }

        return Task.FromResult<IList<IBatchContainer>>(_empty);
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
    /// <param name="microserviceAndTenant">The <see cref="MicroserviceAndTenant"/> the stream belongs to.</param>
    /// <param name="events"><see cref="AppendedEvent">Events</see> to add.</param>
    /// <param name="requestContext">The request context.</param>
    public void AddAppendedEvent(Guid streamGuid, MicroserviceAndTenant microserviceAndTenant, IEnumerable<AppendedEvent> events, IDictionary<string, object> requestContext)
    {
        if (!events.Any())
        {
            return;
        }

        lock (_eventBatches)
        {
            _eventBatches.Add(new EventSequenceBatchContainer(events, streamGuid, microserviceAndTenant.MicroserviceId, microserviceAndTenant.TenantId, requestContext));
        }
    }
}
