// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Diagnostics;

#pragma warning disable RCS1075 // Telemetry failures are deliberately swallowed: reporting a suspicion must never affect the observer

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The suspected-skip reporting of the observer. Everything here is observation only: it is synchronous, adds no
/// await, never touches the observer state and never lets a failure of its own escape into the observer.
/// </summary>
public partial class Observer
{
    readonly SuspectedSkipWarningLimiter _skipWarningLimiter = new();
    EventSkipDetector? _skipDetector;

    EventSkipDetector Skips => _skipDetector ??= new(IsOwnedByObserver, ReportSuspectedSkip);

    static bool IsOfInterest(ObserverSubscription subscription, bool subscribesToAllEvents, AppendedEvent @event)
    {
        if (!subscribesToAllEvents && !subscription.EventTypes.Any(_ => _.Id == @event.Context.EventType.Id))
        {
            return false;
        }

        if (subscription.Filters is not { } filters)
        {
            return true;
        }

        if (filters.EventSourceType is { } eventSourceType &&
            eventSourceType != EventSourceType.Unspecified &&
            @event.Context.EventSourceType != eventSourceType)
        {
            return false;
        }

        if (filters.EventStreamType is { } eventStreamType &&
            !eventStreamType.IsAll &&
            @event.Context.EventStreamType != eventStreamType)
        {
            return false;
        }

        return !filters.Tags.Any() || @event.Context.Tags.Any(_ => filters.Tags.Contains(_.Value));
    }

    bool IsOwnedByObserver(Key partition) =>
        State.CatchingUpPartitions.Contains(partition) ||
        State.ReplayingPartitions.Contains(partition) ||
        Failures.IsFailed(partition);

    /// <summary>
    /// Gets why live events for a partition are held back right now, mirroring the precedence of <see cref="ShouldHandleEvent"/> without its side effects.
    /// </summary>
    /// <param name="partition">The partition.</param>
    /// <returns>The reason, or null if the events are not held back for a reason that catch-up or replay answers.</returns>
    SuspectedSkipReason? HeldBackReasonFor(Key partition)
    {
        if (!CanHoldBackForCatchUpOrReplay(partition))
        {
            return null;
        }

        if (_isPreparingCatchup)
        {
            return SuspectedSkipReason.HeldBackWhilePreparingCatchUp;
        }

        if (State.ReplayingPartitions.Contains(partition))
        {
            return SuspectedSkipReason.HeldBackWhileReplaying;
        }

        return State.CatchingUpPartitions.Contains(partition) ? SuspectedSkipReason.HeldBackWhileCatchingUp : null;
    }

    bool CanHoldBackForCatchUpOrReplay(Key partition) =>
        IsObserverLive() && _subscription.IsSubscribed && !Failures.IsFailed(partition) && IsActiveAndNotQuarantined();

    bool IsObserverLive() => !IsRetired && !_removed && _observerExists;

    bool IsActiveAndNotQuarantined() => !IsQuarantined && State.RunningState == ObserverRunningState.Active;

    void NoteHeldBackEvents(Key partition, IEnumerable<AppendedEvent> events)
    {
        try
        {
            if (HeldBackReasonFor(partition) is not { } reason)
            {
                return;
            }

            var sequenceNumbers = events
                .Where(_ => IsOfInterest(_subscription, State.SubscribesToAllEvents, _))
                .Select(_ => _.Context.SequenceNumber)
                .ToArray();
            if (sequenceNumbers.Length > 0)
            {
                Skips.HeldBack(partition, sequenceNumbers, reason);
            }
        }
        catch (Exception ex) when (ObservationOnlyFailures.CanBeContained(ex))
        {
            // Observation only: a failure here must never change what the observer does with the events.
        }
    }

    void NoteDiscardedEvents(Key partition, IEnumerable<AppendedEvent> events, EventSequenceNumber next)
    {
        try
        {
            var discarded = events
                .Where(_ => _.Context.SequenceNumber < next && IsOfInterest(_subscription, State.SubscribesToAllEvents, _))
                .Select(_ => _.Context.SequenceNumber)
                .ToArray();
            if (discarded.Length > 0)
            {
                Skips.DiscardedBelowNext(partition, discarded, next);
            }
        }
        catch (Exception ex) when (ObservationOnlyFailures.CanBeContained(ex))
        {
            // Observation only: a failure here must never change what the observer does with the events.
        }
    }

    void EvaluateSuspectedSkips() => Skips.Evaluate(State.NextEventSequenceNumber);

    /// <summary>
    /// Releases the ownership that catch-up preparation held over live events, then evaluates what the position has passed.
    /// </summary>
    void NotePreparationEnded()
    {
        Skips.PreparationEnded();
        EvaluateSuspectedSkips();
    }

    void SeedSuspectedSkipOwnership() =>
        Skips.SeedJobOwned(State.InFlightPartitions.Concat(State.CatchingUpPartitions).Concat(State.ReplayingPartitions).ToArray());

    /// <summary>
    /// Moves the observer's last handled event on for a partition, remembering how far the position moved so a later
    /// discard of another partition's event can be put in context.
    /// </summary>
    /// <param name="partition">The partition whose job reported the event.</param>
    /// <param name="lastHandledEvent">The last handled event.</param>
    void HandleNewLastHandledEventForPartition(Key partition, EventSequenceNumber lastHandledEvent)
    {
        var before = State.NextEventSequenceNumber;
        HandleNewLastHandledEvent(lastHandledEvent);
        Skips.Advanced(partition, before, State.NextEventSequenceNumber);
    }

    void ReportSuspectedSkip(Key partition, EventSequenceNumber first, int count, SuspectedSkipReason reason)
    {
        // The measurement and the warning are independent: either of them failing must neither stop the other
        // nor reach the observer.
        try
        {
            _metrics?.EventsSuspectedSkipped(count);
        }
        catch (Exception ex) when (ObservationOnlyFailures.CanBeContained(ex))
        {
            // Observation only.
        }

        try
        {
            if (!_skipWarningLimiter.TryAdmit(out var suppressed))
            {
                return;
            }

            if (suppressed > 0)
            {
                logger.SuspectedSkipWarningsSuppressed(suppressed, _skipWarningLimiter.WindowSeconds);
            }

            logger.SuspectedSkippedEvents(count, partition, first, reason, State.NextEventSequenceNumber);
        }
        catch (Exception ex) when (ObservationOnlyFailures.CanBeContained(ex))
        {
            // Observation only.
        }
    }
}
