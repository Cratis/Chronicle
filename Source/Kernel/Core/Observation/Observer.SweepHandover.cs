// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.StateMachines;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <summary>
    /// The number of the most recent live events kept while a catch-up sweep holds back live delivery.
    /// </summary>
    /// <remarks>
    /// Only the events above what the sweep read are handed over, and under a steady stream of appends those are the
    /// most recent ones, so the bound only has to span what is appended between the sweep's last read and its report.
    /// When it does not, the observer catches up again from where the sweep stopped, and that sweep is shorter.
    /// </remarks>
    internal const int MaximumEventsHeldBackDuringSweep = 5000;

    SweepHeldBackEvents? _sweepHeldBack;
    ulong _catchUpPreparationEpoch;
    SweepHandover? _sweepHandover;

    /// <summary>
    /// The outcome of handing what a sweep held back to the subscriber.
    /// </summary>
    enum SweepHandoverOutcome
    {
        /// <summary>Everything from the observer's position on is handled or still to be delivered live.</summary>
        HandedOver = 0,

        /// <summary>What was held back does not account for everything from the position on; catch up again.</summary>
        NotCovered = 1,

        /// <summary>The observer changed under the handover, which stopped without losing anything not yet handed over.</summary>
        Interrupted = 2
    }

    /// <summary>
    /// Gets the number of live events kept while a catch-up sweep holds back live delivery.
    /// </summary>
    internal int EventsHeldBackForSweep => _sweepHeldBack?.Count ?? 0;

    /// <summary>
    /// Gets whether this observer is caught up in one sweep of the event sequence across every partition. The catch-up
    /// job shapes its steps from the same answer, through <see cref="CatchesUpInOneSweep"/>.
    /// </summary>
    /// <returns>True if catch-up sweeps every partition in one walk.</returns>
    bool CatchesUpInOneSweepNow() => _subscription.CatchesUpInOneSweep(Definition.Type);

    /// <summary>
    /// Raises the hold catch-up preparation keeps over live delivery, keeping what a sweep holds back.
    /// </summary>
    void RaiseCatchUpPreparation()
    {
        _isPreparingCatchup = true;
        _catchUpPreparationEpoch++;
        _sweepHeldBack = CatchesUpInOneSweepNow() ? new(MaximumEventsHeldBackDuringSweep) : null;
    }

    /// <summary>
    /// Lowers the hold catch-up preparation keeps over live delivery, letting go of whatever a sweep held back. Every
    /// path that lowers the hold comes through here, so nothing keeps the held-back events alive once it is down.
    /// </summary>
    void LowerCatchUpPreparation()
    {
        _isPreparingCatchup = false;
        _catchUpPreparationEpoch++;
        _sweepHeldBack = null;
    }

    void KeepEventsHeldBackForSweep(Key partition, IEnumerable<AppendedEvent> events)
    {
        if (_isPreparingCatchup)
        {
            _sweepHeldBack?.Add(partition, events);
        }
    }

    /// <summary>
    /// Lets go of what a sweep held back once the queue subscription that delivered it is gone; what is appended
    /// before the next subscription is never delivered, so the kept events no longer account for everything.
    /// </summary>
    /// <param name="state">The state being left.</param>
    void ForgetEventsHeldBackForSweepWhenLeaving(IState<ObserverState> state)
    {
        if (state is Observing)
        {
            _sweepHeldBack = null;
        }
    }

    /// <summary>
    /// Hands the events live delivery held back above where a sweep stopped to the subscriber, in the order they were
    /// delivered, within the turn that concludes the sweep.
    /// </summary>
    /// <param name="heldBack">The events held back while the sweep was in flight.</param>
    /// <param name="preparationEpoch">The epoch of the hold the held-back events were taken under.</param>
    /// <returns>The <see cref="SweepHandoverOutcome"/>.</returns>
    /// <remarks>
    /// <para>
    /// No event is lost: the sweep read everything below the position, <see cref="SweepHeldBackEvents.Covers"/>
    /// confirms everything delivered from the position on was kept, and anything appended later is delivered live
    /// after this turn - live delivery does not interleave with it. No event is handled twice: the sweep stopped
    /// below the position, the handed-over events are handled once here, and live delivery only ever delivers an
    /// event once. Each partition's events keep their order, because they are handed over in the order they were
    /// delivered and later deliveries come after them.
    /// </para>
    /// <para>
    /// The position is held as the floor while handing over, the same way a page spanning partitions is settled,
    /// so a partition handed over later is not discarded because an earlier one moved the position past it.
    /// </para>
    /// </remarks>
    async Task<SweepHandoverOutcome> HandHeldBackEventsToLiveDelivery(SweepHeldBackEvents heldBack, ulong preparationEpoch)
    {
        var from = State.NextEventSequenceNumber;
        var subscription = _subscription;
        if (!heldBack.Covers(from) ||
            IsQuarantined ||
            State.IsReplaying ||
            _pageFloor is not null ||
            await GetCurrentState() is not Observing)
        {
            logger.HeldBackEventsDoNotCoverTheGap(from);
            return SweepHandoverOutcome.NotCovered;
        }

        var batches = heldBack.From(from).ToArray();
        if (batches.Length == 0)
        {
            return SweepHandoverOutcome.HandedOver;
        }

        logger.HandingHeldBackEventsToLiveDelivery(batches.Sum(_ => _.Events.Length), from);
        Skips.RoundConcluded(batches.Max(_ => _.Events.Max(e => e.Context.SequenceNumber.Value)));

        _pageFloor = from;
        var next = 0;
        var positionMovedByHandover = from;
        try
        {
            for (; next < batches.Length; next++)
            {
                // Every await below lets another call in - a rescue, a fast-forward, an unsubscription, a new
                // catch-up. Handing over on top of any of them would deliver against a world that is no longer this
                // round's, so the handover stops and leaves what it has not handed over to be caught up again.
                if (!await SweepHandoverStillOwnsObserver(preparationEpoch, subscription))
                {
                    await RewindToWhatWasNotHandedOver(batches, next, positionMovedByHandover);
                    logger.SweepHandoverInterrupted(State.NextEventSequenceNumber);
                    return SweepHandoverOutcome.Interrupted;
                }

                var (partition, events) = batches[next];
                if (!ShouldHandleEvent(partition, ignoringCatchUpPreparation: true))
                {
                    continue;
                }

                await HandleEventsForPartition(partition, events, null);
                positionMovedByHandover = State.NextEventSequenceNumber;
            }

            if (!await SweepHandoverStillOwnsObserver(preparationEpoch, subscription))
            {
                logger.SweepHandoverInterrupted(State.NextEventSequenceNumber);
                return SweepHandoverOutcome.Interrupted;
            }
        }
        catch (Exception ex)
        {
            // Batches are handed over one after another, so none after this one has been handed over yet, but an earlier
            // partition's batch may have moved the position past this one's events. The position goes back to the first
            // event not yet handed over; catching up from there may deliver events of earlier batches again, which is
            // preferable to leaving this one's behind.
            _pageFloor = null;
            var notHandedOver = (EventSequenceNumber)batches.Skip(next).Min(_ => _.Events.Min(e => e.Context.SequenceNumber.Value));
            if (notHandedOver < State.NextEventSequenceNumber)
            {
                State = State with { NextEventSequenceNumber = notHandedOver };
                await WriteStateAsync();
            }

            logger.HandingHeldBackEventsFailed(State.NextEventSequenceNumber, ex);
            return SweepHandoverOutcome.NotCovered;
        }
        finally
        {
            _pageFloor = null;
            EvaluateSuspectedSkips();
        }

        return SweepHandoverOutcome.HandedOver;
    }

    /// <summary>
    /// Gets whether the handover still acts on the observer it started on: not retired, removed, quarantined or
    /// replaying, still observing on the same subscription, and with the hold it took the held-back events under still
    /// raised - not lowered by another path, nor raised again by a newer catch-up.
    /// </summary>
    /// <param name="preparationEpoch">The epoch of the hold the handover took the held-back events under.</param>
    /// <param name="subscription">The subscription the handover started on.</param>
    /// <returns>True if the handover may go on.</returns>
    async Task<bool> SweepHandoverStillOwnsObserver(ulong preparationEpoch, ObserverSubscription subscription)
    {
        bool Unchanged() =>
            !IsRetired &&
            !_removed &&
            !IsQuarantined &&
            !State.IsReplaying &&
            _isPreparingCatchup &&
            _catchUpPreparationEpoch == preparationEpoch &&
            ReferenceEquals(_subscription, subscription);

        return Unchanged() && await GetCurrentState() is Observing && Unchanged();
    }

    /// <summary>
    /// Moves the position back to the first event an interrupted handover did not hand over, so whatever drives the
    /// observer next catches up from there. A position something else moved - a fast-forward, a catch-up concluding -
    /// belongs to that path and is left alone.
    /// </summary>
    /// <param name="batches">The batches being handed over.</param>
    /// <param name="next">The index of the first batch not handed over.</param>
    /// <param name="positionMovedByHandover">Where the handover itself last left the position.</param>
    /// <returns>Awaitable task.</returns>
    async Task RewindToWhatWasNotHandedOver((Key Partition, AppendedEvent[] Events)[] batches, int next, EventSequenceNumber positionMovedByHandover)
    {
        if (State.NextEventSequenceNumber != positionMovedByHandover) return;
        var notHandedOver = (EventSequenceNumber)batches.Skip(next).Min(_ => _.Events.Min(e => e.Context.SequenceNumber.Value));
        if (notHandedOver < State.NextEventSequenceNumber)
        {
            State = State with { NextEventSequenceNumber = notHandedOver };
            await WriteStateAsync();
        }
    }

    /// <summary>
    /// Concludes a round whose handover the observer changed under. A hold lowered or raised again by another path
    /// belongs to that path, which routes or rescues on its own; a hold still this round's comes down here, and the
    /// observer is routed only if it is still observing - whatever moved it elsewhere drives it from there.
    /// </summary>
    /// <param name="preparationEpoch">The epoch of the hold the handover took the held-back events under.</param>
    /// <returns>Awaitable task.</returns>
    async Task ConcludeInterruptedSweepHandover(ulong preparationEpoch)
    {
        if (!_isPreparingCatchup || _catchUpPreparationEpoch != preparationEpoch) return;
        LowerCatchUpPreparation();
        if (IsRetired || _removed || IsQuarantined) return;
        if (await GetCurrentState() is Observing)
        {
            await TransitionTo<Routing>();
        }
    }

    /// <summary>
    /// Marks a handover of held-back events in progress, so a report interleaving with it defers to it.
    /// </summary>
    /// <param name="jobId">The job whose report is being handed over.</param>
    /// <param name="lastHandled">The last handled event sequence number it reported.</param>
    /// <param name="lastScanned">The last scanned event sequence number it reported.</param>
    /// <returns>The handover in progress.</returns>
    SweepHandover BeginSweepHandover(JobId jobId, EventSequenceNumber lastHandled, EventSequenceNumber lastScanned) =>
        _sweepHandover = new(jobId, lastHandled, lastScanned);

    /// <summary>
    /// Ends a handover of held-back events, then concludes the reports that arrived while it ran, in arrival order.
    /// </summary>
    /// <param name="handover">The handover ending.</param>
    /// <returns>Awaitable task.</returns>
    async Task EndSweepHandover(SweepHandover handover)
    {
        if (ReferenceEquals(_sweepHandover, handover))
        {
            _sweepHandover = null;
        }

        while (handover.Deferred.TryDequeue(out var conclude))
        {
            if (IsRetired || _removed) return;
            await conclude();
        }
    }

    /// <summary>
    /// Defers a catch-up report that arrives while held-back events are being handed over. The same report again is a
    /// duplicate and changes nothing; any other is concluded once the handover has ended.
    /// </summary>
    /// <param name="jobId">The reporting job.</param>
    /// <param name="releasable">The partition holds the report releases.</param>
    /// <param name="lastHandled">The last handled event sequence number reported.</param>
    /// <param name="lastScanned">The last scanned event sequence number reported.</param>
    /// <returns>True if the report is ignored or deferred; false if no handover is in progress.</returns>
    bool DeferredBySweepHandover(JobId jobId, ISet<Key> releasable, EventSequenceNumber lastHandled, EventSequenceNumber lastScanned)
    {
        if (_sweepHandover is not { } handover) return false;
        if (handover.Job == jobId && handover.LastHandled == lastHandled && handover.LastScanned == lastScanned)
        {
            logger.IgnoringCaughtUpReportedAgainDuringHandover(jobId);
            return true;
        }

        logger.DeferringCaughtUpUntilHandoverEnds(jobId);
        handover.Deferred.Enqueue(() => ConcludeCaughtUpJob(jobId, releasable, lastHandled, lastScanned));
        return true;
    }

    /// <summary>
    /// A handover of held-back events in progress, and the reports deferred until it ends.
    /// </summary>
    /// <param name="job">The job whose report is being handed over.</param>
    /// <param name="lastHandled">The last handled event sequence number it reported.</param>
    /// <param name="lastScanned">The last scanned event sequence number it reported.</param>
    sealed class SweepHandover(JobId job, EventSequenceNumber lastHandled, EventSequenceNumber lastScanned)
    {
        public JobId Job { get; } = job;
        public EventSequenceNumber LastHandled { get; } = lastHandled;
        public EventSequenceNumber LastScanned { get; } = lastScanned;
        public Queue<Func<Task>> Deferred { get; } = new();
    }
}
