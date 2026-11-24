// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Diagnostics;
using Cratis.Chronicle.Observation.ObserverRouting;
using Microsoft.Extensions.Options;
using Orleans.Placement;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IAppendedEventsQueues"/>.
/// </summary>
/// <remarks>
/// The grain holds no routing state: an observer's queue is a stable hash of its identifier, and the queues a batch goes to
/// are read from the silo-local routing table. Nothing is seeded on activation, so the grain is collected like any other
/// when no events are appended.
/// </remarks>
/// <param name="options"><see cref="ChronicleOptions"/> for configuration.</param>
/// <param name="observers">The silo-local <see cref="IObservers"/> routing table.</param>
[PreferLocalPlacement]
public class AppendedEventsQueues(IOptions<ChronicleOptions> options, IObservers observers) : Grain, IAppendedEventsQueues
{
    IAppendedEventsQueue[] _queues = [];
    AppendedEventsQueueRouter _router = null!;
    EventSequenceKey _eventSequenceKey = EventSequenceKey.NotSet;
    bool _isResetting;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var queueCount = options.Value.Events.Queues;
        _eventSequenceKey = EventSequenceKey.Parse(this.GetPrimaryKeyString());
        _queues = Enumerable.Range(0, queueCount).Select(_ => GrainFactory.GetGrain<IAppendedEventsQueue>(_, this.GetPrimaryKeyString())).ToArray();
        _router = new AppendedEventsQueueRouter(queueCount);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task ResetForDevelopment()
    {
        _isResetting = true;
        await Task.WhenAll(_queues.Select(queue => queue.ResetForDevelopment()));
    }

    /// <inheritdoc/>
    public async Task Enqueue(IEnumerable<AppendedEvent> appendedEvents)
    {
        if (_isResetting)
        {
            return;
        }

        var batch = appendedEvents as IReadOnlyList<AppendedEvent> ?? appendedEvents.ToArray();
        if (batch.Count == 0)
        {
            return;
        }

        await Parallel.ForEachAsync(await GetTargetQueues(batch), async (queue, _) => await queue.Enqueue(batch));
    }

    /// <inheritdoc/>
    public async Task<AppendedEventsQueueSubscription> Subscribe(ObserverKey observerKey, EventTypeSet eventTypes, ObserverFilters? filters = null)
    {
        var queueIndex = _router.GetQueueIndexFor(observerKey);
        var subscription = new AppendedEventsQueueSubscription(observerKey, queueIndex);
        if (!_isResetting)
        {
            await _queues[queueIndex].Subscribe(observerKey, eventTypes, filters);
        }

        return subscription;
    }

    /// <inheritdoc/>
    public async Task<AppendedEventsQueueSubscription> SubscribeToAllEventTypes(ObserverKey observerKey, ObserverFilters? filters = null)
    {
        var queueIndex = _router.GetQueueIndexFor(observerKey);
        var subscription = new AppendedEventsQueueSubscription(observerKey, queueIndex);
        if (!_isResetting)
        {
            await _queues[queueIndex].SubscribeToAllEventTypes(observerKey, filters);
        }

        return subscription;
    }

    /// <inheritdoc/>
    public Task Unsubscribe(AppendedEventsQueueSubscription subscription) =>
        _queues[subscription.Queue].Unsubscribe(subscription.ObserverKey);

    /// <inheritdoc/>
    public async Task SpillToCatchup() =>
        await Parallel.ForEachAsync(_queues, async (queue, _) => await queue.SpillToCatchup());

    /// <inheritdoc/>
    public async Task SpillMatchingToCatchup(IEnumerable<AppendedEvent> appendedEvents)
    {
        if (_isResetting) return;
        var batch = appendedEvents as IReadOnlyList<AppendedEvent> ?? appendedEvents.ToArray();
        if (batch.Count == 0) return;
        await Parallel.ForEachAsync(await GetTargetQueues(batch), async (queue, _) => await queue.SpillMatchingToCatchup(batch));
    }

    /// <inheritdoc/>
    public Task<bool> TryMarkDormant(ObserverKey observerKey) =>
        _isResetting ? Task.FromResult(false) : _queues[_router.GetQueueIndexFor(observerKey)].TryMarkDormant(observerKey);

    /// <inheritdoc/>
    public Task<bool> IsDormant(ObserverKey observerKey) =>
        _isResetting ? Task.FromResult(false) : _queues[_router.GetQueueIndexFor(observerKey)].IsDormant(observerKey);

    /// <inheritdoc/>
    public Task Forget(ObserverKey observerKey) =>
        _queues[_router.GetQueueIndexFor(observerKey)].Unsubscribe(observerKey);

    /// <inheritdoc/>
    public async Task<bool> IsSubscribed(ObserverKey observerKey)
    {
        var subscriptions = await _queues[_router.GetQueueIndexFor(observerKey)].GetSubscriptions();
        return subscriptions.Any(subscription => subscription.ObserverKey == observerKey);
    }

    async Task<IEnumerable<IAppendedEventsQueue>> GetTargetQueues(IReadOnlyList<AppendedEvent> batch)
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

        var table = await GetRoutingTable();
        return _router.GetQueuesToDeliverTo(table, _eventSequenceKey.EventSequenceId, batchEventTypeIds).Select(queueIndex => _queues[queueIndex]);
    }

    async Task<ObserverRoutingTable?> GetRoutingTable()
    {
        try
        {
            return await observers.GetTable(_eventSequenceKey.EventStore);
        }
        catch (Exception exception) when (ObservationOnlyFailures.CanBeContained(exception))
        {
            // Without the table every queue receives the batch; each queue filters what is not for its observers.
            return null;
        }
    }
}
