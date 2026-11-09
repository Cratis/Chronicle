// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Partial for delivering a page of events spanning partitions to a subscriber in one call and settling each partition on its own.
/// </summary>
public partial class Observer
{
    readonly Dictionary<Key, PrefetchedDelivery> _prefetchedPartitions = [];
    EventSequenceNumber? _pageFloor;

    /// <inheritdoc/>
    public async Task HandlePage(IReadOnlyList<ObserverPartitionBatch> batches)
    {
        var merged = MergeByPartition(batches);
        if (merged.Count <= 1 || !_subscription.DeliversPartitionPages)
        {
            foreach (var batch in batches)
            {
                await Handle(batch.Partition, batch.Events);
            }

            return;
        }

        // The cursor is single, but the partitions of a page are settled one at a time in whatever order. Every partition is filtered
        // against where the cursor stood when the page arrived, and settling only ever moves the cursor forward.
        _pageFloor = State.NextEventSequenceNumber;
        try
        {
            await DeliverPage(merged);
            foreach (var batch in merged.OrderBy(_ => _.Events[0].Context.SequenceNumber))
            {
                await Handle(batch.Partition, batch.Events);
            }
        }
        finally
        {
            _pageFloor = null;
            _prefetchedPartitions.Clear();
        }
    }

    static List<ObserverPartitionBatch> MergeByPartition(IReadOnlyList<ObserverPartitionBatch> batches)
    {
        var merged = new List<ObserverPartitionBatch>();
        var indexes = new Dictionary<Key, int>();
        foreach (var batch in batches)
        {
            if (indexes.TryGetValue(batch.Partition, out var index))
            {
                merged[index] = merged[index] with { Events = [.. merged[index].Events, .. batch.Events] };
                continue;
            }

            indexes[batch.Partition] = merged.Count;
            merged.Add(batch);
        }

        return merged;
    }

    EventSequenceNumber CursorAfter(EventSequenceNumber candidate) =>
        _pageFloor is null || candidate > State.NextEventSequenceNumber ? candidate : State.NextEventSequenceNumber;

    EventSequenceNumber TailAfter(EventSequenceNumber candidate) =>
        _pageFloor is null || State.TailEventSequenceNumber == EventSequenceNumber.Unavailable || candidate > State.TailEventSequenceNumber ? candidate : State.TailEventSequenceNumber;

    static List<List<ObserverPartitionBatch>> WithinLiveBudget(Observers configuration, IReadOnlyList<ObserverPartitionBatch> candidates)
    {
        // Partitions are taken in the order they arrived and cut into pages that each stay within the live delivery budget. A
        // partition larger than the budget is never part of a page, so it does not keep the partitions around it apart: Handle
        // hands it to catch-up, as it does any single partition over the budget. A partition left on its own is delivered by Handle.
        var pages = new List<List<ObserverPartitionBatch>>();
        var current = new List<ObserverPartitionBatch>();
        var events = 0;
        foreach (var candidate in candidates.Where(_ => !ExceedsLiveBudget(configuration, _.Events.Count)))
        {
            if (current.Count > 0 && ExceedsLiveBudget(configuration, events + candidate.Events.Count))
            {
                pages.Add(current);
                current = [];
                events = 0;
            }

            current.Add(candidate);
            events += candidate.Events.Count;
        }

        pages.Add(current);
        return [.. pages.Where(_ => _.Count > 1 && !ExceedsLiveBudget(configuration, _.Sum(batch => batch.Events.Count)))];
    }

    async Task DeliverPage(IReadOnlyList<ObserverPartitionBatch> batches)
    {
        var candidates = FindPageCandidates(batches);
        if (candidates.Count < 2)
        {
            return;
        }

        var configuration = await configurationProvider.GetFor(_observerKeyText);
        foreach (var page in WithinLiveBudget(configuration, candidates))
        {
            await DeliverPageWithinBudget(page, configuration);
            if (!_observerExists)
            {
                return;
            }
        }
    }

    async Task DeliverPageWithinBudget(List<ObserverPartitionBatch> candidates, Observers configuration)
    {
        logger.DeliveringPage(candidates.Sum(_ => _.Events.Count), candidates.Count);

        // Every partition of the page is in flight until it is settled, and that is made durable before the subscriber sees any of it,
        // so a silo that dies in between drives catch-up on the next activation.
        State = State with { InFlightPartitions = new HashSet<Key>(State.InFlightPartitions.Concat(candidates.Select(_ => _.Partition))) };
        RecordedHandlingState();
        await WriteStateAsync();
        if (!_observerExists)
        {
            return;
        }

        DeliveryFenceContext? fence = null;
        using (new WriteSuspension(this))
        {
            try
            {
                var decrypted = new List<ObserverPartitionBatch>();
                foreach (var candidate in candidates)
                {
                    decrypted.Add(candidate with { Events = await DecryptEvents(candidate.Events) });
                }

                var subscriberTimeout = GetLiveSubscriberTimeout(configuration.GetSubscriberTimeout());
                var hold = await GetClientDeliveryHold();
                var target = subscriberSelector.Select(_subscription, candidates[0].Partition);
                var subscriber = (IObserverPartitionPageSubscriber)GrainFactory.GetGrain(_subscription.SubscriberType, _subscription.GetSubscriberKeyFor(ObserverSubscriberKey.AllPartitions, target.SiloAddress));
                var context = new ObserverSubscriberContext(target.ConnectedClient ?? _subscription.Arguments) { Origin = ObservationOrigin.Live }.WithDeadlines(subscriberTimeout, hold, _clock);
                fence = AbandonedDeliveryDrain.Capture(_subscription, ObserverSubscriberKey.AllPartitions, target, context);
                var pageResult = await subscriber.OnNextPartitionsWithin(subscriberTimeout, decrypted, context);
                foreach (var candidate in candidates)
                {
                    _prefetchedPartitions[candidate.Partition] = pageResult.Results.TryGetValue(candidate.Partition, out var result)
                        ? new(result, null, fence)
                        : new(ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "The subscriber did not answer for the partition"), null, fence);
                }
            }
            catch (Exception ex)
            {
                foreach (var candidate in candidates)
                {
                    _prefetchedPartitions[candidate.Partition] = new(null, ex, fence);
                }
            }
        }
    }

    List<ObserverPartitionBatch> FindPageCandidates(IReadOnlyList<ObserverPartitionBatch> batches)
    {
        var candidates = new List<ObserverPartitionBatch>();
        var floor = _pageFloor ?? State.NextEventSequenceNumber;
        var now = _clock.GetUtcNow();
        foreach (var batch in batches)
        {
            var events = batch.Events.Where(_ => _.Context.SequenceNumber >= floor).ToArray();
            if (events.Length == 0 ||
                !ShouldHandleEvent(batch.Partition) ||
                _deliveryFences.IsHeld(batch.Partition, now) ||
                (!State.SubscribesToAllEvents && !events.Any(_ => _subscription.EventTypes.Any(et => et.Id == _.Context.EventType.Id))) ||
                ExcludedByFilters(events))
            {
                continue;
            }

            candidates.Add(batch with { Events = events });
        }

        return candidates;
    }

    /// <summary>
    /// Gets whether the subscription's filters leave out every one of the events, the same way <see cref="HandleEventsForPartition"/>
    /// skips a partition without delivering it.
    /// </summary>
    /// <param name="events">The events of one partition.</param>
    /// <returns>True if the filters exclude all of them; otherwise false.</returns>
    bool ExcludedByFilters(IReadOnlyCollection<AppendedEvent> events)
    {
        if (_subscription.Filters is not { } filters)
        {
            return false;
        }

        return (filters.EventSourceType is { } eventSourceType && eventSourceType != EventSourceType.Unspecified && !events.Any(_ => _.Context.EventSourceType == eventSourceType)) ||
            (filters.EventStreamType is { } eventStreamType && !eventStreamType.IsAll && !events.Any(_ => _.Context.EventStreamType == eventStreamType)) ||
            (filters.Tags.Any() && !events.Any(_ => _.Context.Tags.Any(t => filters.Tags.Contains(t.Value))));
    }

    sealed record PrefetchedDelivery(ObserverSubscriberResult? Result, Exception? Failure, DeliveryFenceContext? Fence);
}
