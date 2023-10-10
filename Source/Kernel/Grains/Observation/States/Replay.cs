// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Grains.Jobs;
using Aksio.Cratis.Kernel.Grains.Observation.Jobs;
using Aksio.Cratis.Kernel.Orleans.StateMachines;
using Aksio.Cratis.Observation;
using Aksio.DependencyInversion;

namespace Aksio.Cratis.Kernel.Grains.Observation.States;

/// <summary>
/// Represents the replay state of an observer.
/// </summary>
public class Replay : BaseObserverState
{
    readonly ObserverKey _observerKey;
    readonly IExecutionContextManager _executionContextManager;
    readonly ProviderFor<IEventSequenceStorage> _eventSequenceStorageProvider;
    readonly IJobsManager _jobsManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="CatchUp"/> class.
    /// </summary>
    /// <param name="observerKey">The <see cref="ObserverKey"/> for the observer.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="eventSequenceStorageProvider">Provider for <see cref="IEventSequenceStorage"/>.</param>
    /// <param name="jobsManager"><see cref="IJobsManager"/> for working with jobs.</param>
    public Replay(
        ObserverKey observerKey,
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventSequenceStorage> eventSequenceStorageProvider,
        IJobsManager jobsManager)
    {
        _observerKey = observerKey;
        _executionContextManager = executionContextManager;
        _eventSequenceStorageProvider = eventSequenceStorageProvider;
        _jobsManager = jobsManager;
    }

    /// <inheritdoc/>
    public override StateName Name => "Replaying";

    /// <inheritdoc/>
    public override ObserverRunningState RunningState => ObserverRunningState.Replaying;

    /// <inheritdoc/>
    protected override IImmutableList<Type> AllowedTransitions => new[]
    {
        typeof(Indexing),
        typeof(Observing)
    }.ToImmutableList();

    /// <inheritdoc/>
    public override async Task<ObserverState> OnEnter(ObserverState state)
    {
        var subscription = await Observer.GetSubscription();

        await _jobsManager.Start<IReplayJob, ReplayRequest>(
            _observerKey.MicroserviceId,
            _observerKey.TenantId,
            JobId.New(),
            new ReplayRequest(
                state.ObserverId,
                _observerKey,
                subscription,
                state.EventTypes));

        return state;
    }

    /// <inheritdoc/>
    public override async Task<ObserverState> OnLeave(ObserverState state)
    {
        // If events have happened since the replay started, we need to transition to Catchup
        // Set the last event sequence number to the last event sequence number of the event sequence
        _executionContextManager.Establish(_observerKey.TenantId, CorrelationId.New(), _observerKey.MicroserviceId);
        var tail = await _eventSequenceStorageProvider().GetTailSequenceNumber(state.EventSequenceId);
        state.NextEventSequenceNumber = tail.Next();
        state.LastHandledEventSequenceNumber = tail;

        return state;
    }
}
