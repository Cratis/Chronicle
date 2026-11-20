// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Diagnostics;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.Reactors.Clients;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.DependencyInjection;
using AppendedEventsQueueObserverSubscription = Cratis.Chronicle.EventSequences.AppendedEventsQueueObserverSubscription;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <summary>
    /// The number of times a woken observer reads from its cursor to the tail before it leaves the rest to the queue.
    /// </summary>
    /// <remarks>
    /// An event appended while the observer was reading was pushed to its queue lane, which delivers it once the activation is
    /// done, so the rounds only shorten the time it spends behind; they are not what keeps it from being lost.
    /// </remarks>
    const int MaxDormantResumeRounds = 3;

    int _dormantResumeMaxEvents = 1000;
    bool _resumingFromDormancy;
    bool _repairDurableSubscriptionPending;

    /// <summary>
    /// Gets whether the observer was activated from a durable subscription and is still resuming from its cursor.
    /// </summary>
    /// <remarks>
    /// While it is, entering <see cref="Observing"/> does not route the observer to catch-up because events are waiting after
    /// its cursor: the resume reads and delivers them in the activation.
    /// </remarks>
    internal bool IsResumingFromDormancy => _resumingFromDormancy;

    /// <summary>
    /// Gets whether the stored state is one an observer can resume from without anybody else owning part of it.
    /// </summary>
    bool IsResumableState =>
        State.RunningState == ObserverRunningState.Active &&
        !State.IsReplaying &&
        State.ReplayingPartitions.Count == 0 &&
        State.CatchingUpPartitions.Count == 0 &&
        State.InFlightPartitions.Count == 0 &&
        !Failures.HasFailedPartitions &&
        State.NextEventSequenceNumber.IsActualValue &&
        !_deliveryFences.Active(_clock.GetUtcNow()).Any();

    /// <summary>
    /// Restores the subscription the observer recorded before it was released, when the activation has none.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Runs before the state machine enters its state, so the machine finds the subscription it would have had. The record is
    /// advisory: each client it names is validated against the clients connected now, and a record that cannot be
    /// restored (no client left, a state that is not clean) is dropped, so a stale one costs this one activation and repairs
    /// itself. Nothing is restored while the cluster is mixed, which is how the observer behaved before it could be released.
    /// </remarks>
    async Task RestoreFromDurableSubscription()
    {
        if (State.DurableSubscription is not { } record ||
            !_observerExists ||
            _subscription.IsSubscribed ||
            !record.Kind.CanBeReleasedWhenDormant() ||
            !DormantReleaseAllowed ||
            CannotReconcileSubscription())
        {
            return;
        }

        if (!IsResumableState || !await TryApplyDurableSubscription(record))
        {
            ForgetDurableSubscription();
            _repairDurableSubscriptionPending = true;
            _metrics?.DormantResumed(DormantResumeOutcomes.Stale);

            // The record said a client held this observer until now, so this is when it was last seen: a client that
            // left while the observer was dormant starts the clock for removing unseen observers here.
            if (record.Kind.IsServedByClient())
            {
                await RecordSeenByClient(force: true);
            }

            return;
        }

        RecordDurableSubscription();
        _resumingFromDormancy = true;
        _wokenFromDormancyAndUnused = true;
    }

    async Task<bool> TryApplyDurableSubscription(ObserverDurableSubscription record)
    {
        var eventTypes = Definition.EventTypes.ToArray();
        var filters = record.Filters?.ToObserverFilters();
        var restored = record.Kind switch
        {
            ObserverSubscriptionKind.ClientReactor when record.DeliversPages => await RestoreClientTargets<IReactorPageObserverSubscriber>(record, eventTypes, filters),
            ObserverSubscriptionKind.ClientReactor => await RestoreClientTargets<IReactorObserverSubscriber>(record, eventTypes, filters),
            ObserverSubscriptionKind.ClientReducer when record.DeliversPages => await RestoreClientTargets<IReducerPageObserverSubscriber>(record, eventTypes, filters),
            ObserverSubscriptionKind.ClientReducer => await RestoreClientTargets<IReducerObserverSubscriber>(record, eventTypes, filters),
            ObserverSubscriptionKind.CollapsingProjection => RestoreKernelSubscription<ICollapsingProjectionObserverSubscriber>(eventTypes, filters),
            ObserverSubscriptionKind.Projection => RestoreKernelSubscription<IProjectionObserverSubscriber>(eventTypes, filters),
            _ => false
        };

        if (!restored)
        {
            return false;
        }

        _eventTypeSchemas = (await storage.GetEventStore(_observerKey.EventStore).EventTypes.GetFor(eventTypes)).ToDictionary(schema => schema.Type);
        return true;
    }

    async Task<bool> RestoreClientTargets<TSubscriber>(ObserverDurableSubscription record, EventType[] eventTypes, ObserverFilters? filters)
        where TSubscriber : IObserverSubscriber
    {
        var restored = false;
        foreach (var target in record.Targets)
        {
            var connectionId = new ConnectionId(target.ConnectionId);
            var silo = SiloAddress.FromParsableString(target.Silo);
            if (await ConnectedClientsLocator.Find(GrainFactory, silo, connectionId) is not { } owner)
            {
                continue;
            }

            var connectedClient = await owner.GetConnectedClient(connectionId);
            ApplySubscription<TSubscriber>(eventTypes, silo, connectedClient, Definition.IsReplayable, filters, Definition.GenerationDelivery);
            restored = true;
        }

        return restored;
    }

    bool RestoreKernelSubscription<TSubscriber>(EventType[] eventTypes, ObserverFilters? filters)
        where TSubscriber : IObserverSubscriber
    {
        var silo = ServiceProvider.GetService<ILocalSiloDetails>()?.SiloAddress ?? SiloAddress.Zero;
        if (State.SubscribesToAllEvents)
        {
            _subscription = new(_observerId, _observerKey, [], typeof(TSubscriber), silo, null, Definition.IsReplayable);
            return true;
        }

        ApplySubscription<TSubscriber>(eventTypes, silo, null, Definition.IsReplayable, filters, Definition.GenerationDelivery);
        return true;
    }

    /// <summary>
    /// Finishes what an activation from a durable subscription started: repairs a stale record, or resumes from the cursor.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    async Task FinishWakeFromDormancy()
    {
        if (_repairDurableSubscriptionPending)
        {
            _repairDurableSubscriptionPending = false;
            await RepairStaleDurableSubscription();
        }

        if (!_resumingFromDormancy)
        {
            return;
        }

        var resumed = false;
        try
        {
            resumed = await ResumeFromCursor();
        }
        catch (Exception ex) when (ObservationOnlyFailures.CanBeContained(ex))
        {
            logger.ResumeFromDormancyFailed(ex);
        }

        // Routing (now without the flag) is what finds the events from the cursor by catch-up when the resume was not clean.
        _resumingFromDormancy = false;
        _metrics?.DormantResumed(resumed ? DormantResumeOutcomes.Cursor : DormantResumeOutcomes.CatchUp);
        if (!resumed)
        {
            await TransitionTo<Routing>();
        }
    }

    async Task RepairStaleDurableSubscription()
    {
        await PersistForgottenDurableSubscription();
        try
        {
            await _appendedEventsQueues.Forget(_observerKey);
        }
        catch (Exception ex) when (ObservationOnlyFailures.CanBeContained(ex))
        {
            logger.ForgettingDormantEntryFailed(ex);
        }
    }

    /// <summary>
    /// Reads from the persisted cursor to the tail and delivers what is found, the way live delivery would.
    /// </summary>
    /// <returns>True when the observer is at the tail or has handed the rest to live delivery; false when the catch-up path must take over.</returns>
    /// <remarks>
    /// The queue subscription was taken when the observer entered <see cref="Observing"/>, before the tail is read here, so
    /// every event is either at or before the tail (read from the log from the cursor) or after it (pushed to the lane, which
    /// delivers it after the activation and discards what the cursor has passed). The events go through <see cref="HandlePage"/>
    /// and <see cref="Handle"/>, the same entry the queue delivers through: a woken observer is a partition with no claim,
    /// which decision 0018 gives to live delivery, and anything that is not that (a failed partition, a claim, a fence, a job)
    /// was ruled out before the observer could be dormant. A gap over <see cref="Configuration.Observers.DormantResumeMaxEvents"/>,
    /// one that would be spread over more partition runs than live delivery accepts, or any read that does not succeed
    /// returns false before anything was delivered.
    /// </remarks>
    async Task<bool> ResumeFromCursor()
    {
        if (State.RunningState != ObserverRunningState.Active || !_subscription.IsSubscribed || !State.NextEventSequenceNumber.IsActualValue)
        {
            return true;
        }

        for (var round = 0; round < MaxDormantResumeRounds; round++)
        {
            var cursor = State.NextEventSequenceNumber;
            var tail = await _eventSequence.GetTailSequenceNumber();
            if (!tail.IsActualValue || cursor > tail)
            {
                return true;
            }

            var gap = await ReadDormantGap(cursor, tail);
            if (gap is null)
            {
                return false;
            }

            if (gap.Count == 0)
            {
                // Nothing after the cursor is of interest, so the cursor moves to the tail, as routing does for a quiet observer.
                // What is appended after the tail arrives by the queue.
                State = State with { NextEventSequenceNumber = tail.Next(), TailEventSequenceNumber = tail };
                await WriteStateAsync();
                return true;
            }

            if (!await DeliverDormantGap(gap) || State.NextEventSequenceNumber <= cursor)
            {
                return false;
            }

            if (!_subscription.IsSubscribed)
            {
                return true;
            }
        }

        return true;
    }

    async Task<List<AppendedEvent>?> ReadDormantGap(EventSequenceNumber cursor, EventSequenceNumber tail)
    {
        var events = new List<AppendedEvent>();
        var sequence = storage.GetEventStore(_observerKey.EventStore).GetNamespace(_observerKey.Namespace).GetEventSequence(_observerKey.EventSequenceId);
        var eventTypes = State.SubscribesToAllEvents ? null : _subscription.EventTypes.ToArray();
        using var cursorOverGap = await sequence.GetRange(cursor, tail, eventTypes: eventTypes);
        while (await cursorOverGap.MoveNext())
        {
            events.AddRange(cursorOverGap.Current);
            if (events.Count > _dormantResumeMaxEvents)
            {
                return null;
            }
        }

        var subscription = new AppendedEventsQueueObserverSubscription(
            _observerKey,
            [.. _subscription.EventTypes.Select(eventType => eventType.Id)],
            _subscription.Filters,
            State.SubscribesToAllEvents);
        return [.. AppendedEventsQueue.SelectForSubscription(events, subscription)];
    }

    async Task<bool> DeliverDormantGap(List<AppendedEvent> gap)
    {
        var runs = AppendedEventsQueue.SplitIntoRuns(gap);
        if (_subscription.DeliversPartitionPages && runs.Count > 1)
        {
            await HandlePage([.. runs.Select(run => new ObserverPartitionBatch(run.Partition, run.Events))]);
            return true;
        }

        if (runs.Count > AppendedEventsQueue.MaxPartitionRunsPerLiveDispatch)
        {
            return false;
        }

        var maxPerCall = await configurationProvider.GetMaxEventsPerSubscriberCallForObserver(_observerKey, _subscription.SubscriberType);
        foreach (var (partition, events) in runs)
        {
            foreach (var chunk in maxPerCall > 0 ? events.Chunk(maxPerCall) : [events])
            {
                await Handle(partition, chunk);
            }
        }

        return true;
    }
}
