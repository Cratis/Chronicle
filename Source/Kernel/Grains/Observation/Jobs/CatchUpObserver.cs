// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Jobs;
using Aksio.Cratis.Kernel.Grains.Jobs;
using Aksio.Cratis.Kernel.Grains.Observation.States;
using Aksio.Cratis.Kernel.Storage;

namespace Aksio.Cratis.Kernel.Grains.Observation.Jobs;

/// <summary>
/// Represents a job for catching up an observer.
/// </summary>
public class CatchUpObserver : Job<CatchUpObserverRequest, CatchUpObserverState>, ICatchUpObserver
{
    readonly IStorage _storage;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayObserver"/> class.
    /// </summary>
    /// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
    public CatchUpObserver(IStorage storage)
    {
        _storage = storage;
    }

    /// <inheritdoc/>
    protected override bool RemoveAfterCompleted => true;

    /// <inheritdoc/>
    public override async Task OnCompleted()
    {
        var observer = GrainFactory.GetGrain<IObserver>(Request.ObserverId, Request.ObserverKey);
        await observer.ReportHandledEvents(State.HandledCount);
        await observer.TransitionTo<Routing>();
    }

    /// <inheritdoc/>
    protected override Task OnStepCompleted(JobStepId jobStepId, JobStepResult result)
    {
        if (result.Result is HandleEventsForPartitionResult handleEventsForPartitionResult)
        {
            State.HandledCount += handleEventsForPartitionResult.HandledEvents;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override JobDetails GetJobDetails() => $"{Request.ObserverId}";

    /// <inheritdoc/>
    protected override Task<bool> CanResume()
    {
        var observer = GrainFactory.GetGrain<IObserver>(Request.ObserverId, Request.ObserverKey);
        return observer.IsSubscribed();
    }

    /// <inheritdoc/>
    protected override async Task<IImmutableList<JobStepDetails>> PrepareSteps(CatchUpObserverRequest request)
    {
        var observerKeyIndexes = _storage.GetEventStore((string)JobKey.MicroserviceId).GetNamespace(JobKey.TenantId).ObserverKeyIndexes;
        var index = await observerKeyIndexes.GetFor(
            request.ObserverId,
            request.ObserverKey);

        var keys = index.GetKeys(request.FromEventSequenceNumber);

        var steps = new List<JobStepDetails>();

        await foreach (var key in keys)
        {
            steps.Add(CreateStep<IHandleEventsForPartition>(
                new HandleEventsForPartitionArguments(
                    request.ObserverId,
                    request.ObserverKey,
                    request.ObserverSubscription,
                    key,
                    request.FromEventSequenceNumber,
                    Events.EventObservationState.None,
                    request.EventTypes)));
        }

        return steps.ToImmutableList();
    }
}
