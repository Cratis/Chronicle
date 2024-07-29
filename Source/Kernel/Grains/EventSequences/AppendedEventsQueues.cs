// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Grains.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IAppendedEventsQueues"/>.
/// </summary>
public class AppendedEventsQueues : Grain, IAppendedEventsQueues
{
    readonly IAppendedEventsQueue[] _queues;
    int _nextQueue;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppendedEventsQueues"/> class.
    /// </summary>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
    public AppendedEventsQueues(IGrainFactory grainFactory)
    {
        _queues = Enumerable.Range(0, 1).Select(_ => grainFactory.GetGrain<IAppendedEventsQueue>(_, this.GetPrimaryKeyString())).ToArray();
    }

    /// <inheritdoc/>
    public Task Enqueue(IEnumerable<AppendedEvent> appendedEvents)
    {
        Parallel.ForEachAsync(_queues, async (queue, ctx) => await queue.Enqueue(appendedEvents));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<AppendedEventsQueueSubscription> Subscribe(ObserverKey observerKey, IEnumerable<EventType> eventTypes)
    {
        var currentQueue = _nextQueue % _queues.Length;
        var subscription = new AppendedEventsQueueSubscription(observerKey, currentQueue);
        await _queues[currentQueue].Subscribe(observerKey, eventTypes);

        _nextQueue++;

        return subscription;
    }

    /// <inheritdoc/>
    public async Task Unsubscribe(AppendedEventsQueueSubscription subscription)
    {
        await _queues[subscription.Queue].Unsubscribe(subscription.ObserverKey);
    }
}
