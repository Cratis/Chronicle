// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.States;

/// <summary>
/// Represents the state of an observer that is catching up partitions which had in-flight events
/// at the time of the last shutdown.
/// </summary>
/// <param name="observerKey">The <see cref="ObserverKey"/> for the observer.</param>
/// <param name="definitionState"><see cref="IPersistentState{ObserverDefinition}"/> for the observer's definition.</param>
/// <param name="failuresState"><see cref="IPersistentState{FailedPartitions}"/> for the observer's failed partitions.</param>
/// <param name="storage"><see cref="IStorage"/> for accessing the in-flight events store.</param>
/// <param name="jobsManager"><see cref="IJobsManager"/> for starting partition catch-up jobs.</param>
/// <param name="logger">Logger for logging.</param>
/// <remarks>
/// On entry, every partition that has an in-flight event marker — but is not already failed,
/// replaying, or catching up — gets a dedicated catch-up job starting from the next event after
/// the observer's last confirmed sequence number. The state then transitions to <see cref="Routing"/>
/// so the observer can resume normal operation. If enqueueing a catch-up job throws, the observer
/// transitions to <see cref="QuarantinedObserver"/> rather than silently losing recovery work.
/// </remarks>
public class CatchingUpInFlight(
    ObserverKey observerKey,
    IPersistentState<ObserverDefinition> definitionState,
    IPersistentState<FailedPartitions> failuresState,
    IStorage storage,
    IJobsManager jobsManager,
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
        using var scope = logger.BeginCatchingUpInFlightScope(state.Identifier, observerKey);

        if (state.RunningState == ObserverRunningState.Quarantined)
        {
            await StateMachine.TransitionTo<Routing>();
            return state;
        }

        var inFlightStorage = storage
            .GetEventStore(observerKey.EventStore)
            .GetNamespace(observerKey.Namespace)
            .InFlightEvents;

        var entries = (await inFlightStorage.GetFor(state.Identifier)).ToArray();
        if (entries.Length == 0)
        {
            await StateMachine.TransitionTo<Routing>();
            return state;
        }

        logger.RecoveringInFlightPartitions(entries.Length);

        var partitions = entries
            .Select(_ => _.Partition)
            .Distinct()
            .Where(p => !state.CatchingUpPartitions.Contains(p))
            .Where(p => !state.ReplayingPartitions.Contains(p))
            .Where(p => !failuresState.State.IsFailed(p))
            .ToArray();

        var startFrom = state.LastHandledEventSequenceNumber.IsActualValue
            ? state.LastHandledEventSequenceNumber.Next()
            : EventSequenceNumber.First;

        try
        {
            foreach (var partition in partitions)
            {
                logger.StartingInFlightCatchUpForPartition(partition, startFrom);
                state.CatchingUpPartitions.Add(partition);
                await jobsManager.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(
                    new(observerKey, definitionState.State.Type, partition, startFrom, definitionState.State.EventTypes));
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

    /// <inheritdoc/>
    /// <remarks>
    /// On leaving the state, no per-state mutable bookkeeping is held — the catching-up partitions are
    /// recorded directly on <see cref="ObserverState"/> and persist across the transition. This hook is
    /// kept as the explicit place to clean up state-specific bookkeeping should any be introduced later.
    /// </remarks>
    public override Task<ObserverState> OnLeave(ObserverState state) => Task.FromResult(state);
}
