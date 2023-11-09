// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Grains.Jobs;
using Aksio.Cratis.Kernel.Persistence.Observation;
using Aksio.Cratis.Observation;
using Aksio.DependencyInversion;
using Orleans.Runtime;

namespace Aksio.Cratis.Kernel.Grains.Observation.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IConsolidateStateForObserver"/>.
/// </summary>
public class ConsolidateStateForObserver : JobStep<ObserverIdAndKey, JobStepState>, IConsolidateStateForObserver
{
    readonly ProviderFor<IObserverStorage> _observerStorageProvider;
    readonly ProviderFor<IEventSequenceStorage> _eventSequenceStorageProvider;
    readonly IExecutionContextManager _executionContextManager;
    IObserver? _observer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsolidateStateForObserver"/> class.
    /// </summary>
    /// <param name="state"><see cref="IPersistentState{TState}"/> for managing state of the job step.</param>
    /// <param name="observerStorageProvider">Provider for <see cref="IObserverStorage"/>.</param>
    /// <param name="eventSequenceStorageProvider">Provider for <see cref="IEventSequenceStorage"/>.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    public ConsolidateStateForObserver(
        [PersistentState(nameof(JobStepState), WellKnownGrainStorageProviders.JobSteps)]
        IPersistentState<JobStepState> state,
        ProviderFor<IObserverStorage> observerStorageProvider,
        ProviderFor<IEventSequenceStorage> eventSequenceStorageProvider,
        IExecutionContextManager executionContextManager)
        : base(state)
    {
        _observerStorageProvider = observerStorageProvider;
        _eventSequenceStorageProvider = eventSequenceStorageProvider;
        _executionContextManager = executionContextManager;
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

        _executionContextManager.Establish(request.ObserverKey.TenantId, _executionContextManager.Current.CorrelationId, request.ObserverKey.MicroserviceId);

        if (state.LastHandledEventSequenceNumber == EventSequenceNumber.Unavailable &&
            state.Handled == EventCount.NotSet)
        {
            var lastHandled = await _eventSequenceStorageProvider().GetTailSequenceNumber(
                    state.EventSequenceId,
                    state.EventTypes);

            if (lastHandled < state.NextEventSequenceNumber)
            {
                lastHandledEventSequenceNumber = lastHandled;
                modified = true;
            }
        }

        if (state.Handled == EventCount.NotSet)
        {
            var count = await _eventSequenceStorageProvider().GetCount(
                state.EventSequenceId,
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

        return JobStepResult.Succeeded;
    }
}
