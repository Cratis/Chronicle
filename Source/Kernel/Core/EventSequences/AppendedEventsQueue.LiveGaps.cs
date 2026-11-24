// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Verifies filtered continuity once when a subscription enters live delivery, before partition or page dispatch.
/// </summary>
public partial class AppendedEventsQueue
{
    /// <summary>
    /// Each (re)subscription has a new identity. Once its first dispatch has no hole, the ordered lane owns
    /// every later matching event until it is closed by unsubscribe/spill. Weak keys do not retain old epochs.
    /// </summary>
    readonly ConditionalWeakTable<AppendedEventsQueueObserverSubscription, VerifiedLiveStart> _verifiedLiveStarts = new();

    async Task<bool> VerifyLiveStart(AppendedEventsQueueObserverLane lane, IReadOnlyList<AppendedEvent> events)
    {
        var subscription = CurrentSubscriptionOf(lane);
        if (_storage is null || _verifiedLiveStarts.TryGetValue(subscription, out _)) return true;

        try
        {
            return await VerifySubscriptionStart(lane, events, subscription);
        }
        catch (OperationCanceledException) when (_catchupCancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception exception) when (ObservationOnlyFailures.CanBeContained(exception))
        {
            SpillFailedSubscriptionToCatchup(subscription, [.. events]);
            LaneFailed(exception);
            return false;
        }
    }

    async Task<bool> VerifySubscriptionStart(AppendedEventsQueueObserverLane lane, IReadOnlyList<AppendedEvent> events, AppendedEventsQueueObserverSubscription subscription)
    {
        var first = events[0].Context.SequenceNumber;
        var state = await lane.Observer.GetState();
        var hasGap = state is { RunningState: ObserverRunningState.Active, NextEventSequenceNumber.IsActualValue: true } &&
            await HasMatchingEventBefore(subscription, state.NextEventSequenceNumber, first);
        if (!ReferenceEquals(subscription, CurrentSubscriptionOf(lane))) return await VerifyLiveStart(lane, events);
        if (hasGap)
        {
            // Stop all delivery, including pages, before asking the normal catch-up path to acquire/adopt
            // job ownership. A job already running cannot make this later live batch safe to dispatch.
            lock (_subscriptionsLock) SpillLaggingSubscriptionToCatchup(subscription);
            return false;
        }

        // A stale batch below the cursor says nothing about the subscription's first new live event.
        if (state is not null && first < state.NextEventSequenceNumber) return true;
        _verifiedLiveStarts.GetValue(subscription, _ => new(first));
        return true;
    }

    async Task<bool> HasMatchingEventBefore(AppendedEventsQueueObserverSubscription subscription, EventSequenceNumber from, EventSequenceNumber first)
    {
        if (from >= first) return false;
        var storage = _storage!.GetEventStore(_eventSequenceKey.EventStore)
            .GetNamespace(_eventSequenceKey.Namespace).GetEventSequence(_eventSequenceKey.EventSequenceId);
        using var cursor = await storage.GetFromSequenceNumber(from, cancellationToken: _catchupCancellationToken);
        while (await cursor.MoveNext())
        {
            foreach (var @event in cursor.Current)
            {
                if (@event.Context.SequenceNumber >= first) return false;
                if (MatchesSubscription(subscription, @event)) return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Cache the proven no-gap boundary for this subscription epoch. Subsequent batches need no storage/state
    /// or namespace-wide job lookup: the lane is serial, and a spill closes it before a hole can be crossed.
    /// </summary>
    /// <param name="Through">The first live sequence number verified against the observer's cursor.</param>
    sealed record VerifiedLiveStart(EventSequenceNumber Through);
}
