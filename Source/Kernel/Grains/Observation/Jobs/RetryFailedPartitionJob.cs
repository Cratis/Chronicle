// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Kernel.Grains.Jobs;

namespace Aksio.Cratis.Kernel.Grains.Observation.Jobs;

/// <summary>
/// Represents a job for retrying a failed partition.
/// </summary>
public class RetryFailedPartitionJob : Job<RetryFailedPartitionRequest, JobState<RetryFailedPartitionRequest>>, IRetryFailedPartitionJob
{
    /// <inheritdoc/>
    protected override bool RemoveAfterCompleted => true;

    /// <inheritdoc/>
    public override async Task OnCompleted()
    {
        if (State.Progress.SuccessfulSteps == 1)
        {
            var observer = GrainFactory.GetGrain<IObserver>(State.Request.ObserverId, State.Request.ObserverKey);
            await observer.FailedPartitionRecovered(State.Request.Key);
        }
    }

    /// <inheritdoc/>
    protected override JobDetails GetJobDetails() => $"{State.Request.ObserverId}-{State.Request.Key}";

    /// <inheritdoc/>
    protected override Task<bool> CanResume()
    {
        if (State.Request is null)
        {
            return Task.FromResult(false);
        }

        var observer = GrainFactory.GetGrain<IObserver>(State.Request.ObserverId, State.Request.ObserverKey);
        return observer.IsSubscribed();
    }

    /// <inheritdoc/>
    protected override Task<IImmutableList<JobStepDetails>> PrepareSteps(RetryFailedPartitionRequest request)
    {
        var steps = new[]
        {
            CreateStep<IHandleEventsForPartition>(
                new HandleEventsForPartitionArguments(
                    request.ObserverId,
                    request.ObserverKey,
                    request.ObserverSubscription,
                    request.Key,
                    request.FromSequenceNumber,
                    Events.EventObservationState.None,
                    request.EventTypes))
        }.ToImmutableList();

        return Task.FromResult<IImmutableList<JobStepDetails>>(steps);
    }
}
