// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;
using Orleans.Runtime;
using Orleans.Streams;

namespace Cratis.Kernel.Grains.EventSequences.Streaming;

/// <summary>
/// Represents an implementation of <see cref="EventSequenceQueueAdapterReceiver"/> for MongoDB event log.
/// </summary>
public class EventSequenceQueueAdapterReceiver : IQueueAdapterReceiver
{
    readonly List<IBatchContainer> _eventBatches = new();
    readonly List<IBatchContainer> _empty = new();

    /// <inheritdoc/>
    public Task<IList<IBatchContainer>> GetQueueMessagesAsync(int maxCount)
    {
        if (_eventBatches.Count != 0)
        {
            var result = _eventBatches.OrderBy(_ => _.SequenceToken).ToArray().ToList();
            _eventBatches.Clear();
            return Task.FromResult<IList<IBatchContainer>>(result);
        }

        return Task.FromResult<IList<IBatchContainer>>(_empty);
    }

    /// <inheritdoc/>
    public Task Initialize(TimeSpan timeout) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task MessagesDeliveredAsync(IList<IBatchContainer> messages) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task Shutdown(TimeSpan timeout) => Task.CompletedTask;

    /// <summary>
    /// Add an appended event to the receivers queue.
    /// </summary>
    /// <param name="streamId">The <see cref="StreamId"/> identifying the stream.</param>
    /// <param name="events"><see cref="AppendedEvent">Events</see> to add.</param>
    /// <param name="requestContext">The request context.</param>
    public void AddAppendedEvent(StreamId streamId, IEnumerable<AppendedEvent> events, IDictionary<string, object> requestContext)
    {
        if (!events.Any())
        {
            return;
        }

        _eventBatches.Add(new EventSequenceBatchContainer(events, streamId, requestContext));
    }
}
