// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Grains.Jobs;
using Aksio.Cratis.Kernel.Storage;
using Aksio.Cratis.Kernel.Storage.EventSequences;
using Aksio.Cratis.Kernel.Storage.Jobs;
using Aksio.Cratis.Observation;
using Orleans.Runtime;

namespace Aksio.Cratis.Kernel.Grains.Observation.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IConsolidateStateForObserver"/>.
/// </summary>
public class ConsolidateStateForObserver : JobStep<ObserverIdAndKey, object, JobStepState>, IConsolidateStateForObserver
{
    readonly IClusterStorage _clusterStorage;
    IEventSequenceStorage? _eventSequenceStorage;
    IObserver? _observer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsolidateStateForObserver"/> class.
    /// </summary>
    /// <param name="state"><see cref="IPersistentState{TState}"/> for managing state of the job step.</param>
    /// <param name="clusterStorage"><see cref="IClusterStorage"/> for accessing storage for the cluster.</param>
    public ConsolidateStateForObserver(
        [PersistentState(nameof(JobStepState), WellKnownGrainStorageProviders.JobSteps)]
        IPersistentState<JobStepState> state,
        IClusterStorage clusterStorage)
        : base(state)
    {
        _clusterStorage = clusterStorage;
    }

    /// <inheritdoc/>
    public override Task Prepare(ObserverIdAndKey request)
    {
        _observer = GrainFactory.GetGrain<IObserver>(request.ObserverId, request.ObserverKey);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override async Task<JobStepResult> PerformStep(ObserverIdAndKey request, CancellationToken cancellationToken)
    {
        var state = await _observer!.GetState();
        var lastHandledEventSequenceNumber = state.LastHandledEventSequenceNumber;
        var handled = state.Handled;

        var modified = false;

        var eventSequenceStorage = GetEventSequenceStorage(request.ObserverKey.MicroserviceId, request.ObserverKey.TenantId, request.ObserverKey.EventSequenceId);

        if (state.LastHandledEventSequenceNumber == EventSequenceNumber.Unavailable &&
            state.Handled == EventCount.NotSet)
        {
            var lastHandled = await eventSequenceStorage.GetTailSequenceNumber(state.EventTypes);

            if (lastHandled < state.NextEventSequenceNumber)
            {
                lastHandledEventSequenceNumber = lastHandled;
                modified = true;
            }
        }

        if (state.Handled == EventCount.NotSet)
        {
            var count = await eventSequenceStorage.GetCount(
                state.LastHandledEventSequenceNumber,
                state.EventTypes);

            var lastHandled = lastHandledEventSequenceNumber;
            if (count == EventCount.Zero)
            {
                lastHandled = EventSequenceNumber.Unavailable;
            }

            handled = count;
            lastHandledEventSequenceNumber = lastHandled;
            modified = true;
        }

        if (modified)
        {
            await _observer!.SetHandledStats(handled, lastHandledEventSequenceNumber);
        }

        return JobStepResult.Succeeded();
    }

    IEventSequenceStorage GetEventSequenceStorage(MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId) => _eventSequenceStorage ??= _clusterStorage.GetEventStore((string)microserviceId).GetInstance(tenantId).GetEventSequence(eventSequenceId);
}
