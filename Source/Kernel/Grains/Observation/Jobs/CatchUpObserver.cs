// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Observation.Jobs;

/// <summary>
/// Represents a job for catching up an observer.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReplayObserver"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
/// <param name="logger">The logger.</param>
public class CatchUpObserver(IStorage storage, ILogger<CatchUpObserver> logger) : Job<CatchUpObserverRequest, JobStateWithLastHandledEvent>, ICatchUpObserver
{
    IObserver? _observer;

    /// <inheritdoc/>
    public override async Task OnCompleted()
    {
        using var scope = logger.BeginJobScope(JobId, JobKey);
        if (!AllStepsCompletedSuccessfully)
        {
            if (State.LastHandledEventSequenceNumber.IsActualValue)
            {
                logger.NotAllEventsWereHandled(nameof(CatchUpObserver), State.LastHandledEventSequenceNumber);
            }
            else
            {
                logger.NoEventsWereHandled(nameof(CatchUpObserver));
            }
        }

        var observer = GrainFactory.GetGrain<IObserver>(Request.ObserverKey);
        await observer.CaughtUp(State.LastHandledEventSequenceNumber);
    }

    /// <inheritdoc/>
    protected override Task OnStepCompleted(JobStepId jobStepId, JobStepResult result)
    {
        State.HandleResult(result);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override JobDetails GetJobDetails() => $"{Request.ObserverKey.ObserverId}";

    /// <inheritdoc/>
    protected override Task<bool> CanResume()
    {
        var observer = GrainFactory.GetGrain<IObserver>(Request.ObserverKey);
        return observer.IsSubscribed();
    }

    /// <inheritdoc/>
    protected override Task OnBeforePrepareSteps(CatchUpObserverRequest request)
    {
        _observer = GrainFactory.GetGrain<IObserver>(Request.ObserverKey);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override async Task<IImmutableList<JobStepDetails>> PrepareSteps(CatchUpObserverRequest request)
    {
        var observerKeyIndexes = storage.GetEventStore(JobKey.EventStore).GetNamespace(JobKey.Namespace).ObserverKeyIndexes;
        var index = await observerKeyIndexes.GetFor(request.ObserverKey);
        var keys = index.GetKeys(request.FromEventSequenceNumber);

        var steps = new List<JobStepDetails>();
        var keysForSteps = new List<Key>();

        await foreach (var key in keys)
        {
            steps.Add(CreateStep<IHandleEventsForPartition>(
                new HandleEventsForPartitionArguments(
                    request.ObserverKey,
                    request.ObserverSubscription,
                    key,
                    request.FromEventSequenceNumber,
                    EventSequenceNumber.Max,
                    EventObservationState.None,
                    request.EventTypes)));
            keysForSteps.Add(key);
        }

        await (_observer?.RegisterCatchingUpPartitions(keysForSteps) ?? Task.CompletedTask);

        return steps.ToImmutableList();
    }
}
