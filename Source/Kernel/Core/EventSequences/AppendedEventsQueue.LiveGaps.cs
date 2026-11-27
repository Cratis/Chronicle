// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.EventSequences;

/// <content>
/// Verifies filtered continuity once when a subscription enters live delivery, before partition or page dispatch.
/// </content>
public partial class AppendedEventsQueue
{
    /// <summary>
    /// Each (re)subscription has a new identity. Once its first dispatch has no hole, the ordered lane owns
    /// every later matching event until it is closed by unsubscribe/spill. Weak keys do not retain old epochs.
    /// </summary>
    readonly ConditionalWeakTable<AppendedEventsQueueObserverSubscription, VerifiedLiveStart> _verifiedLiveStarts = new();

    static EventType[]? LiveGapEventTypes(AppendedEventsQueueObserverSubscription subscription) => subscription.AllEventTypes
        ? null
        : [.. subscription.EventTypeIdSet.Append(GlobalEventTypes.Redaction).Distinct().Select(id => new EventType(id, EventTypeGeneration.First))];

    void RegisterLiveSubscription(AppendedEventsQueueObserverSubscription subscription)
    {
        // Subscribe holds the subscriptions lock. Retarget an existing lane immediately, not only when the
        // next batch is routed, so an in-flight read cannot certify the replaced subscription's filters.
        _subscriptions.Add(subscription);
        if (_lanes.TryGetValue(subscription.ObserverKey, out var lane)) lane.Subscription = subscription;
    }

    /// <summary>
    /// The delivery every live lane is created with: no event reaches the observer before its subscription proved a gap-free
    /// live start. Lanes are the only live-dispatch path, and a lazily rung observer re-enters through a new subscription.
    /// </summary>
    /// <param name="lane">The <see cref="AppendedEventsQueueObserverLane"/> of the observer.</param>
    /// <param name="events">The events, in sequence order.</param>
    /// <returns>Awaitable task.</returns>
    async Task DeliverVerifiedLiveEvents(AppendedEventsQueueObserverLane lane, IReadOnlyList<AppendedEvent> events)
    {
        if (await VerifyLiveStart(lane, events)) await DeliverToObserver(lane, events);
    }

    async Task<bool> VerifyLiveStart(AppendedEventsQueueObserverLane lane, IReadOnlyList<AppendedEvent> events)
    {
        if (_storage is null) return true;
        while (!lane.IsClosed && !_catchupCancellationToken.IsCancellationRequested)
        {
            var subscription = CurrentSubscriptionOf(lane);
            var result = await TryVerifySubscriptionStart(lane, events, subscription);
            if (result.HasValue) return result.Value;
        }

        return false;
    }

    async Task<bool?> TryVerifySubscriptionStart(AppendedEventsQueueObserverLane lane, IReadOnlyList<AppendedEvent> events, AppendedEventsQueueObserverSubscription subscription)
    {
        try
        {
            lock (_subscriptionsLock)
            {
                if (ReferenceEquals(subscription, lane.Subscription) && _verifiedLiveStarts.TryGetValue(subscription, out _)) return true;
            }

            return await VerifySubscriptionStart(lane, events, subscription);
        }
        catch (OperationCanceledException) when (_catchupCancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception exception) when (ObservationOnlyFailures.CanBeContained(exception))
        {
            lock (_subscriptionsLock)
            {
                if (!ReferenceEquals(subscription, lane.Subscription)) return null;
                SpillFailedSubscriptionToCatchup(subscription, [.. events]);
            }

            LaneFailed(exception);
            return false;
        }
    }

    async Task<bool?> VerifySubscriptionStart(AppendedEventsQueueObserverLane lane, IReadOnlyList<AppendedEvent> events, AppendedEventsQueueObserverSubscription subscription)
    {
        var state = await lane.Observer.GetState();
        if (state is not { RunningState: ObserverRunningState.Active, NextEventSequenceNumber.IsActualValue: true })
        {
            return CompleteLiveStart(lane, subscription, null, null);
        }

        // Eligibility is by sequence number alone: every event at or past the cursor is dispatched, and the observer
        // advances its cursor past a run even when none of it matches (an inapplicable run). A replacement subscription
        // whose filters exclude the whole queued batch must therefore still be verified up to the batch's last event.
        var eligible = events.Where(@event => @event.Context.SequenceNumber >= state.NextEventSequenceNumber).ToArray();
        if (eligible.Length == 0) return CompleteLiveStart(lane, subscription, null, null);

        var gap = await FindLiveStartGap(subscription, state.NextEventSequenceNumber, eligible);
        return CompleteLiveStart(lane, subscription, eligible[^1].Context.SequenceNumber, gap);
    }

    bool? CompleteLiveStart(AppendedEventsQueueObserverLane lane, AppendedEventsQueueObserverSubscription subscription, EventSequenceNumber? through, LiveStartGap? gap)
    {
        lock (_subscriptionsLock)
        {
            if (lane.IsClosed) return false;
            if (!ReferenceEquals(subscription, lane.Subscription)) return null;
            if (gap is not null)
            {
                // Partition recovery does not prove every matching hole belongs to that partition. Conservatively
                // stop live delivery; normal catch-up acquires/adopts ownership without trusting the later batch.
                _logger.LiveStartDeliveryGap(subscription.ObserverKey, gap.From, gap.FirstLive, gap.LastLive, gap.Missing);
                SpillLaggingSubscriptionToCatchup(subscription, AppendedEventsQueueSpillReasons.LiveDeliveryGap);
                return false;
            }

            // A stale-only batch (or an inactive observer) proves no new boundary and must not consume the check.
            if (through is not null) _verifiedLiveStarts.GetValue(subscription, _ => new(through));
            return true;
        }
    }

    async Task<LiveStartGap?> FindLiveStartGap(AppendedEventsQueueObserverSubscription subscription, EventSequenceNumber from, AppendedEvent[] eligible)
    {
        var last = eligible[^1].Context.SequenceNumber;
        if (from >= last) return null;
        var delivered = eligible.Select(@event => @event.Context.SequenceNumber).ToHashSet();
        var storage = _storage!.GetEventStore(_eventSequenceKey.EventStore)
            .GetNamespace(_eventSequenceKey.Namespace).GetEventSequence(_eventSequenceKey.EventSequenceId);
        using var cursor = await storage.GetRange(
            from,
            last - 1,
            eventSourceId: null,
            eventTypes: LiveGapEventTypes(subscription),
            tags: subscription.Filters?.Tags.Select(tag => new Tag(tag)),
            eventSourceType: subscription.Filters?.EventSourceType,
            eventStreamType: subscription.Filters?.EventStreamType,
            cancellationToken: _catchupCancellationToken);
        while (await cursor.MoveNext())
        {
            foreach (var @event in cursor.Current)
            {
                // Coalescing can also hide a hole between two new events in the same batch. Already-present
                // live events are not holes, but every other stored match must be recovered before dispatch.
                if (MatchesSubscription(subscription, @event) && !delivered.Contains(@event.Context.SequenceNumber))
                {
                    return new(from, eligible[0].Context.SequenceNumber, last, @event.Context.SequenceNumber);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// The first stored matching event absent from the live batch, with the cursor and batch boundaries used to find it.
    /// </summary>
    /// <param name="From">The observer's next event sequence number.</param>
    /// <param name="FirstLive">The first cursor-eligible live event sequence number.</param>
    /// <param name="LastLive">The last live event sequence number, just beyond the probed range.</param>
    /// <param name="Missing">The first missing matching event sequence number returned by storage.</param>
    sealed record LiveStartGap(EventSequenceNumber From, EventSequenceNumber FirstLive, EventSequenceNumber LastLive, EventSequenceNumber Missing);

    /// <summary>
    /// Cache the proven no-gap boundary for this subscription epoch. Subsequent batches need no storage/state
    /// or namespace-wide job lookup: the lane is serial, and a spill closes it before a hole can be crossed.
    /// </summary>
    /// <param name="Through">The last cursor-eligible live sequence number verified against the observer's cursor.</param>
    sealed record VerifiedLiveStart(EventSequenceNumber Through);
}
