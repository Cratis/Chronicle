// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Grains.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IAppendedEventsQueues"/>.
/// </summary>
/// <param name="options"><see cref="ChronicleOptions"/> for configuration.</param>
[KeepAlive]
public class AppendedEventsQueues(IOptions<ChronicleOptions> options) : Grain, IAppendedEventsQueues
{
    IAppendedEventsQueue[] _queues = [];
    int _nextQueue;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _queues = Enumerable.Range(0, options.Value.Events.Queues).Select(_ => GrainFactory.GetGrain<IAppendedEventsQueue>(_, this.GetPrimaryKeyString())).ToArray();

        return Task.CompletedTask;
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
