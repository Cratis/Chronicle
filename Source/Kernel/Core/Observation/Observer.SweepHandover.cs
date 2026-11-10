// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

    /// <summary>
    /// Gets whether this observer is caught up in one sweep of the event sequence across every partition. The catch-up
    /// job shapes its steps from the same answer, through <see cref="CatchesUpInOneSweep"/>.
    /// </summary>
    /// <returns>True if catch-up sweeps every partition in one walk.</returns>
    bool CatchesUpInOneSweepNow() => _subscription.CatchesUpInOneSweep(Definition.Type);

    void StartKeepingEventsHeldBackForSweep() =>
        _sweepHeldBack = CatchesUpInOneSweepNow() ? new(MaximumEventsHeldBackDuringSweep) : null;

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
    /// <returns>True if everything from the observer's position on is handled or still to be delivered live; false if
    /// the observer has to catch up again from its position.</returns>
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
    async Task<bool> HandHeldBackEventsToLiveDelivery(SweepHeldBackEvents heldBack)
    {
        var from = State.NextEventSequenceNumber;
        if (!heldBack.Covers(from) ||
            IsQuarantined ||
            State.IsReplaying ||
            _pageFloor is not null ||
            await GetCurrentState() is not Observing)
        {
            logger.HeldBackEventsDoNotCoverTheGap(from);
            return false;
        }

        var batches = heldBack.From(from).ToArray();
        if (batches.Length == 0)
        {
            return true;
        }

        logger.HandingHeldBackEventsToLiveDelivery(batches.Sum(_ => _.Events.Length), from);
        Skips.RoundConcluded(batches.Max(_ => _.Events.Max(e => e.Context.SequenceNumber.Value)));

        _pageFloor = from;
        var next = 0;
        try
        {
            for (; next < batches.Length; next++)
            {
                if (IsRetired || _removed) return false;
                var (partition, events) = batches[next];
                if (!ShouldHandleEvent(partition, ignoringCatchUpPreparation: true))
                {
                    continue;
                }

                await HandleEventsForPartition(partition, events, null);
            }
        }
        catch (Exception ex)
        {
            // A partition handed over after this one may already have moved the position past it, so the position goes
            // back to the first event not yet handed over. Catching up from there may deliver a later partition's events
            // again, which is preferable to leaving this one's behind.
            _pageFloor = null;
            var notHandedOver = (EventSequenceNumber)batches.Skip(next).Min(_ => _.Events.Min(e => e.Context.SequenceNumber.Value));
            if (notHandedOver < State.NextEventSequenceNumber)
            {
                State = State with { NextEventSequenceNumber = notHandedOver };
                await WriteStateAsync();
            }

            logger.HandingHeldBackEventsFailed(State.NextEventSequenceNumber, ex);
            return false;
        }
        finally
        {
            _pageFloor = null;
            EvaluateSuspectedSkips();
        }

        return true;
    }
}
