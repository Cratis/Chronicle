// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.States;

/// <summary>
/// Represents the state of an observer that is catching up partitions which had in-flight events
/// at the time of the last shutdown.
/// </summary>
/// <param name="observerKey">The <see cref="ObserverKey"/> for the observer.</param>
/// <param name="definitionState"><see cref="IPersistentState{ObserverDefinition}"/> for the observer's definition.</param>
/// <param name="failuresState"><see cref="IPersistentState{FailedPartitions}"/> for the observer's failed partitions.</param>
/// <param name="jobsManager"><see cref="IJobsManager"/> for starting partition catch-up jobs.</param>
/// <param name="storage"><see cref="IStorage"/> for finding the jobs that own held partitions.</param>
/// <param name="logger">Logger for logging.</param>
/// <remarks>
/// On entry, every partition recorded as in-flight on the observer state — but that is not already failed,
/// replaying, or catching up — gets a dedicated catch-up job starting from the next event after
/// the observer's last confirmed sequence number. The state then transitions to <see cref="Routing"/>
/// so the observer can resume normal operation. If enqueueing a catch-up job fails or throws, the observer
/// transitions to <see cref="QuarantinedObserver"/> rather than silently losing recovery work. Markers are
/// relinquished only when a durable job or an existing recovery mechanism owns the partition.
/// </remarks>
public class CatchingUpInFlight(
    ObserverKey observerKey,
    IPersistentState<ObserverDefinition> definitionState,
    IPersistentState<FailedPartitions> failuresState,
    IJobsManager jobsManager,
    IStorage storage,
    ILogger<CatchingUpInFlight> logger) : BaseObserverState
{
    /// <inheritdoc/>
    public override ObserverRunningState RunningState => ObserverRunningState.Unknown;

    /// <inheritdoc/>
    protected override IImmutableList<Type> AllowedTransitions => new[]
    {
        typeof(Routing),
        typeof(QuarantinedObserver),
        typeof(Disconnected)
    }.ToImmutableList();

    /// <inheritdoc/>
    public override async Task<ObserverState> OnEnter(ObserverState state)
    {
        using var scope = logger.BeginCatchingUpInFlightScope(observerKey.ObserverId, observerKey);

        if (state.RunningState == ObserverRunningState.Quarantined)
        {
            await StateMachine.TransitionTo<Routing>();
            return state;
        }

        if (state.InFlightPartitions.Count == 0)
        {
            await StateMachine.TransitionTo<Routing>();
            return state;
        }

        logger.RecoveringInFlightPartitions(state.InFlightPartitions.Count);

        var claims = StateMachine as IObserverOwnershipClaims;

        // A catch-up hold is granted and persisted before its job starts, so a crash in between leaves a hold no job owns.
        // Only a durable job relieves the marker; without one, recovery restarts from the saved position.
        var owned = await PartitionsOwnedByDurableCatchUp(state);
        var partitions = state.InFlightPartitions
            .Where(p =>
                !(state.CatchingUpPartitions.Contains(p) && (owned is null || owned.Contains(p))) &&
                !state.ReplayingPartitions.Contains(p) &&
                !failuresState.State.IsFailed(p))
            .ToArray();

        // Other recovery mechanisms own the skipped markers. Keep a private set until each new job is durable. A marker
        // whose ownership could not be looked up is kept for the next activation to decide.
        var undecided = owned is null ? state.InFlightPartitions.Where(state.CatchingUpPartitions.Contains) : [];
        state = state with { InFlightPartitions = partitions.Concat(undecided).ToHashSet() };

        var startFrom = state.LastHandledEventSequenceNumber.IsActualValue
            ? state.LastHandledEventSequenceNumber.Next()
            : EventSequenceNumber.First;

        try
        {
            // The holds and claims are persisted before any job starts, so admission finds the claim each job is stamped with.
            var epochs = claims is null || partitions.Length == 0
                ? new Dictionary<Key, ulong?>().AsReadOnly()
                : await claims.GrantPartitionsBeforeStart(partitions, ClaimHolderKind.PartitionCatchUp, startFrom);
            foreach (var partition in partitions)
            {
                logger.StartingInFlightCatchUpForPartition(partition, startFrom);
                state.CatchingUpPartitions.Add(partition);
                var epoch = epochs.GetValueOrDefault(partition);
                var startResult = await jobsManager.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(
                    new(observerKey, definitionState.State.Type, partition, startFrom, definitionState.State.EventTypes)
                    {
                        ReaderScope = ObserverReaderScope.For(state.SubscribesToAllEvents, definitionState.State.EventTypes),
                        OwnershipEpoch = epoch
                    });

                // A start refused because a later grant holds the partition is owned by the catch-up started under it.
                if (startResult?.TryGetResult(out _) is not true && claims?.IsHeldByNewerClaim(partition, epoch) is not true)
                {
                    var error = startResult?.TryGetError(out var startError) is true ? startError : default;
                    logger.CouldNotStartInFlightCatchUpForPartition(partition, error);
                    await StateMachine.TransitionTo<QuarantinedObserver>();
                    return state;
                }

                state.InFlightPartitions.Remove(partition);
            }
        }
        catch (Exception ex)
        {
            logger.FailedToCatchUpInFlightPartitions(ex);
            await StateMachine.TransitionTo<QuarantinedObserver>();
            return state;
        }

        await StateMachine.TransitionTo<Routing>();
        return state;
    }

    /// <summary>
    /// Finds the in-flight partitions already held for catch-up that an outstanding partition catch-up job owns.
    /// </summary>
    /// <param name="state">The <see cref="ObserverState"/>.</param>
    /// <returns>The owned partitions; empty when no in-flight partition is held, and null when the jobs could not be looked up.</returns>
    async Task<ISet<Key>?> PartitionsOwnedByDurableCatchUp(ObserverState state)
    {
        var held = state.InFlightPartitions.Count(state.CatchingUpPartitions.Contains);
        if (held == 0) return new HashSet<Key>();

        try
        {
            var result = await storage.GetEventStore(observerKey.EventStore).GetNamespace(observerKey.Namespace).Jobs.GetOutstandingJobs();
            if (result.TryGetResult(out var jobs))
            {
                return jobs.PartitionsHeldBy<CatchUpObserverPartitionRequest>(observerKey, DateTimeOffset.UtcNow, state);
            }
        }
        catch (Exception ex)
        {
            logger.InFlightCatchUpOwnershipLookupThrew(ex);
        }

        logger.KeepingInFlightMarkersJobLookupFailed(held);
        return null;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// On leaving the state, no per-state mutable bookkeeping is held — the catching-up partitions are
    /// recorded directly on <see cref="ObserverState"/> and persist across the transition. This hook is
    /// kept as the explicit place to clean up state-specific bookkeeping should any be introduced later.
    /// </remarks>
    public override Task<ObserverState> OnLeave(ObserverState state) => Task.FromResult(state);
}
