// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;
using Orleans.Placement;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IAppendedEventsQueues"/>.
/// </summary>
/// <param name="options"><see cref="ChronicleOptions"/> for configuration.</param>
[KeepAlive]
[PreferLocalPlacement]
public class AppendedEventsQueues(IOptions<ChronicleOptions> options) : Grain, IAppendedEventsQueues
{
    IAppendedEventsQueue[] _queues = [];
    AppendedEventsQueueRouter _router = null!;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var queueCount = options.Value.Events.Queues;
        _queues = Enumerable.Range(0, queueCount).Select(_ => GrainFactory.GetGrain<IAppendedEventsQueue>(_, this.GetPrimaryKeyString())).ToArray();
        _router = new AppendedEventsQueueRouter(queueCount);
        await SeedRouter();
    }

    /// <inheritdoc/>
    public async Task Enqueue(IEnumerable<AppendedEvent> appendedEvents)
    {
        var batch = appendedEvents as IReadOnlyList<AppendedEvent> ?? appendedEvents.ToArray();
        if (batch.Count == 0)
        {
            return;
        }

        await Parallel.ForEachAsync(GetTargetQueues(batch), async (queue, _) => await queue.Enqueue(batch));
    }

    /// <inheritdoc/>
    public async Task<AppendedEventsQueueSubscription> Subscribe(ObserverKey observerKey, IEnumerable<EventType> eventTypes, ObserverFilters? filters = null)
    {
        var eventTypesList = eventTypes as IReadOnlyCollection<EventType> ?? eventTypes.ToArray();
        var queueIndex = _router.Subscribe(observerKey, eventTypesList.Select(eventType => eventType.Id));
        var subscription = new AppendedEventsQueueSubscription(observerKey, queueIndex);
        await _queues[queueIndex].Subscribe(observerKey, eventTypesList, filters);
        return subscription;
    }

    /// <inheritdoc/>
    public async Task Unsubscribe(AppendedEventsQueueSubscription subscription)
    {
        _router.Unsubscribe(subscription.Queue, subscription.ObserverKey);
        await _queues[subscription.Queue].Unsubscribe(subscription.ObserverKey);
    }

    async Task SeedRouter()
    {
        for (var queueIndex = 0; queueIndex < _queues.Length; queueIndex++)
        {
            try
            {
                var subscriptions = await _queues[queueIndex].GetSubscriptions();
                _router.Seed(queueIndex, subscriptions);
            }
            catch
            {
                // A queue left unseeded keeps receiving every batch until it is reconciled: a missed delivery
                // would silently drop events, whereas an extra delivery is idempotent for observers.
            }
        }
    }

    IEnumerable<IAppendedEventsQueue> GetTargetQueues(IReadOnlyList<AppendedEvent> batch)
    {
        var batchEventTypeIds = new HashSet<EventTypeId>();
        foreach (var appendedEvent in batch)
        {
            var eventTypeId = appendedEvent.Context.EventType.Id;

            // A redaction stands in for the original event type when matched by observers. Rather than duplicate
            // that matching here, route redaction batches to every queue — they are rare — and let the queues filter.
            if (eventTypeId == GlobalEventTypes.Redaction)
            {
                return _queues;
            }

            batchEventTypeIds.Add(eventTypeId);
        }

        return _router.GetQueuesToDeliverTo(batchEventTypeIds).Select(queueIndex => _queues[queueIndex]);
    }
}
