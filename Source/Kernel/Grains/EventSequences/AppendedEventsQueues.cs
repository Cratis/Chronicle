// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Grains.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IAppendedEventsQueues"/>.
/// </summary>
public class AppendedEventsQueues : Grain, IAppendedEventsQueues
{
    IAppendedEventsQueue[] _queues = [];
    int _nextQueue;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // Keep the Grain alive forever: Confirmed here: https://github.com/dotnet/orleans/issues/1721#issuecomment-216566448
        DelayDeactivation(TimeSpan.MaxValue);

        _queues = Enumerable.Range(0, 8).Select(_ => GrainFactory.GetGrain<IAppendedEventsQueue>(_, this.GetPrimaryKeyString())).ToArray();

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
