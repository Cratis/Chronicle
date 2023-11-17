// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Grains.Jobs;
using Aksio.Cratis.Kernel.Grains.Observation.States;
using Aksio.Cratis.Kernel.Keys;

namespace Aksio.Cratis.Kernel.Grains.Observation.Jobs;

/// <summary>
/// Represents a job for replaying an observer.
/// </summary>
public class ReplayObserver : Job<ReplayObserverRequest, ReplayObserverState>, IReplayObserver
{
    readonly IObserverKeyIndexes _observerKeyIndexes;
    ReplayObserverRequest? _request;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayObserver"/> class.
    /// </summary>
    /// <param name="observerKeyIndexes"><see cref="IObserverKeyIndexes"/> for getting index for observer to replay.</param>
    public ReplayObserver(IObserverKeyIndexes observerKeyIndexes)
    {
        _observerKeyIndexes = observerKeyIndexes;
    }

    /// <inheritdoc/>
    public override async Task OnCompleted()
    {
        if (_request == null) return;
        var observer = GrainFactory.GetGrain<IObserver>(_request.ObserverId, _request.ObserverKey);
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
    protected override JobDetails GetJobDetails(ReplayObserverRequest request) => $"{request.ObserverId}";

    /// <inheritdoc/>
    protected override Task<bool> CanResume()
    {
        var observer = GrainFactory.GetGrain<IObserver>(State.Request.ObserverId, State.Request.ObserverKey);
        return observer.IsSubscribed();
    }

    /// <inheritdoc/>
    protected override async Task<IImmutableList<JobStepDetails>> PrepareSteps(ReplayObserverRequest request)
    {
        _request = request;
        var index = await _observerKeyIndexes.GetFor(
            request.ObserverId,
            request.ObserverKey);

        var keys = index.GetKeys(EventSequenceNumber.First);
        var steps = new List<JobStepDetails>();

        await foreach (var key in keys)
        {
            steps.Add(CreateStep<IHandleEventsForPartition>(
                new HandleEventsForPartitionArguments(
                    request.ObserverId,
                    request.ObserverKey,
                    request.ObserverSubscription,
                    key,
                    EventSequenceNumber.First,
                    EventObservationState.Replay,
                    request.EventTypes)));
        }

        return steps.ToImmutableList();
    }
}
