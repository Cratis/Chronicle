// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Jobs;
using Orleans.Runtime;

namespace Cratis.Chronicle.Grains.Observation.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IConsolidateStateForObserver"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ConsolidateStateForObserver"/> class.
/// </remarks>
/// <param name="jobStepState"><see cref="IPersistentState{TState}"/> for managing state of the job step.</param>
/// <param name="storage"><see cref="IStorage"/> for accessing storage for the cluster.</param>
public class ConsolidateStateForObserver(
    [PersistentState(nameof(JobStepState), WellKnownGrainStorageProviders.JobSteps)]
    IPersistentState<JobStepState> jobStepState,
    IStorage storage) : JobStep<ObserverIdAndKey, object, JobStepState>(jobStepState), IConsolidateStateForObserver
{
    IEventSequenceStorage? _eventSequenceStorage;
    IObserver? _observer;

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

        var eventSequenceStorage = GetEventSequenceStorage(request.ObserverKey.EventStore, request.ObserverKey.Namespace, request.ObserverKey.EventSequenceId);

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

    IEventSequenceStorage GetEventSequenceStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId) =>
        _eventSequenceStorage ??= storage.GetEventStore(eventStore).GetNamespace(@namespace).GetEventSequence(eventSequenceId);
}
