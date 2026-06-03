// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IAppendedEventsQueues"/>.
/// </summary>
/// <param name="options"><see cref="ChronicleOptions"/> for configuration.</param>
[KeepAlive]
public class AppendedEventsQueues(IOptions<ChronicleOptions> options) : Grain, IAppendedEventsQueues
{
    IAppendedEventsQueue[] _queues = [];

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _queues = Enumerable.Range(0, options.Value.Events.Queues).Select(_ => GrainFactory.GetGrain<IAppendedEventsQueue>(_, this.GetPrimaryKeyString())).ToArray();

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Enqueue(IEnumerable<AppendedEvent> appendedEvents) =>
        await Parallel.ForEachAsync(_queues, async (queue, ctx) => await queue.Enqueue(appendedEvents));

    /// <inheritdoc/>
    public async Task<AppendedEventsQueueSubscription> Subscribe(ObserverKey observerKey, IEnumerable<EventType> eventTypes, ObserverFilters? filters = null)
    {
        // Deterministic hash-based queue assignment ensures the same observer always lands on
        // the same queue across subscribe/unsubscribe cycles. The previous round-robin counter
        // moved an observer to a different queue every time the Observing state was re-entered
        // (e.g. after a catch-up cycle), leaving brief windows where dispatched events landed
        // on a queue the observer was no longer subscribed to — surfacing as silently dropped
        // events for projections that aggregate across event sources.
        var queueIndex = GetQueueIndexFor(observerKey);
        var subscription = new AppendedEventsQueueSubscription(observerKey, queueIndex);
        await _queues[queueIndex].Subscribe(observerKey, eventTypes, filters);
        return subscription;
    }

    /// <inheritdoc/>
    public async Task Unsubscribe(AppendedEventsQueueSubscription subscription)
    {
        await _queues[subscription.Queue].Unsubscribe(subscription.ObserverKey);
    }

    int GetQueueIndexFor(ObserverKey observerKey)
    {
        // Use the observer identifier as the hash input — it is stable for a given observer
        // and produces a deterministic queue assignment.
        var hash = observerKey.ObserverId.Value.GetHashCode(StringComparison.Ordinal);
        return (int)((uint)hash % (uint)_queues.Length);
    }
}
