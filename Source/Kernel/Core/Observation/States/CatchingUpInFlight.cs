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
        var startFrom = state.LastHandledEventSequenceNumber.IsActualValue
            ? state.LastHandledEventSequenceNumber.Next()
            : EventSequenceNumber.First;

        // A catch-up hold is granted and persisted before its job starts, so a crash in between leaves a hold no job owns.
        // Only a durable job relieves the marker; without one, recovery restarts from the saved position.
        var (owned, observerCatchUp) = await HeldPartitionOwnership(state, startFrom);
        var partitions = state.InFlightPartitions
            .Where(p =>
                !(state.CatchingUpPartitions.Contains(p) && owned?.Contains(p) != false) &&
                !observerCatchUp.Contains(p) &&
                !state.ReplayingPartitions.Contains(p) &&
                !failuresState.State.IsFailed(p))
            .ToArray();

        // Other recovery mechanisms own the skipped markers. Keep a private set until each new job is durable. A marker
        // whose ownership could not be looked up is kept for the next activation to decide, and a marker left to the
        // observer-wide catch-up is kept for that job.
        var kept = owned is null ? state.InFlightPartitions.Where(state.CatchingUpPartitions.Contains) : observerCatchUp;
        state = state with { InFlightPartitions = partitions.Concat(kept).ToHashSet() };

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

                    // NotAdmitted refuses before persisting or starting the new job (Cratis.Orleans 1.32.1 Job.Start).
                    // Other errors, including Unknown, may follow dispatched steps; keep their markers as with exceptions.
                    // TODO(#4496): reconcile durable jobs before retrying ambiguous starts.
                    if (error == StartJobError.NotAdmitted)
                    {
                        state.CatchingUpPartitions.Remove(partition);
                    }
                    await StateMachine.TransitionTo<QuarantinedObserver>();
                    return state;
                }

                state.InFlightPartitions.Remove(partition);
            }
        }
        catch (Exception ex)
        {
            logger.FailedToCatchUpInFlightPartitions(ex);

            // Start threw, so a durable job may already exist for the in-flight partition; the marker is kept deliberately.
            // TODO(#4496): reconcile durable jobs before retrying, then roll back markers that no job owns.
            await StateMachine.TransitionTo<QuarantinedObserver>();
            return state;
        }

        await StateMachine.TransitionTo<Routing>();
        return state;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// On leaving the state, no per-state mutable bookkeeping is held — the catching-up partitions are
    /// recorded directly on <see cref="ObserverState"/> and persist across the transition. This hook is
    /// kept as the explicit place to clean up state-specific bookkeeping should any be introduced later.
    /// </remarks>
    public override Task<ObserverState> OnLeave(ObserverState state) => Task.FromResult(state);

    /// <summary>
    /// Finds which outstanding catch-up job owns each in-flight partition already held for catch-up.
    /// </summary>
    /// <param name="state">The <see cref="ObserverState"/>.</param>
    /// <param name="startFrom">The position a new partition catch-up would start from.</param>
    /// <returns>
    /// The partitions owned by a partition catch-up job, or null when the jobs could not be looked up; and the partitions
    /// left to the observer-wide catch-up job. Both are empty when no in-flight partition is held.
    /// </returns>
    async Task<(ISet<Key>? Owned, ISet<Key> ObserverCatchUp)> HeldPartitionOwnership(ObserverState state, EventSequenceNumber startFrom)
    {
        var held = state.InFlightPartitions.Where(state.CatchingUpPartitions.Contains).ToArray();
        if (held.Length == 0) return (new HashSet<Key>(), new HashSet<Key>());

        try
        {
            var result = await storage.GetEventStore(observerKey.EventStore).GetNamespace(observerKey.Namespace).Jobs.GetOutstandingJobs();
            if (result.TryGetResult(out var jobs))
            {
                var now = DateTimeOffset.UtcNow;
                return (
                    jobs.PartitionsHeldBy<CatchUpObserverPartitionRequest>(observerKey, now, state),
                    PartitionsLeftToObserverCatchUp(jobs, state, held, startFrom, now));
            }
        }
        catch (Exception ex)
        {
            logger.InFlightCatchUpOwnershipLookupThrew(ex);
        }

        logger.KeepingInFlightMarkersJobLookupFailed(held.Length);
        return (null, new HashSet<Key>());
    }

    /// <summary>
    /// Finds the held partitions that the outstanding observer-wide catch-up job registered and still owns.
    /// </summary>
    /// <param name="jobs">The outstanding jobs.</param>
    /// <param name="state">The <see cref="ObserverState"/>.</param>
    /// <param name="held">The in-flight partitions held for catch-up.</param>
    /// <param name="startFrom">The position a new partition catch-up would start from.</param>
    /// <param name="now">The current time.</param>
    /// <returns>The partitions left to the observer-wide catch-up job.</returns>
    /// <remarks>
    /// A hold granted for a partition catch-up always has its partition claim, persisted before its job starts, so a held
    /// partition without one was registered by the observer-wide catch-up. That job owns it only while it is outstanding
    /// under the observer-wide claim's epoch and resumes no later than a new partition catch-up would start.
    /// </remarks>
    HashSet<Key> PartitionsLeftToObserverCatchUp(IEnumerable<JobState> jobs, ObserverState state, IEnumerable<Key> held, EventSequenceNumber startFrom, DateTimeOffset now)
    {
        var wide = state.ObserverWideClaim;
        if (wide is not { Holder: ClaimHolderKind.ObserverCatchUp, Job: JobId jobId } ||
            wide.ResumePosition.Value > startFrom.Value ||
            !jobs.Any(job =>
                job.Id == jobId &&
                job.Request is CatchUpObserverRequest request &&
                request.ObserverKey == observerKey &&
                request.OwnershipEpoch == wide.Epoch.Value &&
                !job.Status.HasFinished() &&
                !JobLiveness.HasConcludedAllStepsLongAgo(job, now)))
        {
            return [];
        }

        var claimedByPartitionCatchUp = (state.PartitionClaims ?? [])
            .Where(_ => _.Holder == ClaimHolderKind.PartitionCatchUp)
            .Select(_ => _.Partition)
            .ToHashSet();
        return held.Where(_ => !claimedByPartitionCatchUp.Contains(_)).ToHashSet();
    }
}
