// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Jobs;
using Aksio.Cratis.Kernel.Grains.Jobs;
using Aksio.Cratis.Kernel.Persistence.Jobs;

namespace Aksio.Cratis.Kernel.Grains.Observation.Jobs;

/// <summary>
/// Represents a job for retrying a failed partition.
/// </summary>
public class RetryFailedPartitionJob : Job<RetryFailedPartitionRequest, JobState>, IRetryFailedPartitionJob
{
    /// <inheritdoc/>
    protected override bool RemoveAfterCompleted => true;

    /// <inheritdoc/>
    public override async Task OnCompleted()
    {
        if (State.Progress.SuccessfulSteps == 1)
        {
            var observer = GrainFactory.GetGrain<IObserver>(Request.ObserverId, Request.ObserverKey);
            await observer.FailedPartitionRecovered(Request.Key);
        }
    }

    /// <inheritdoc/>
    protected override JobDetails GetJobDetails() => $"{Request.ObserverId}-{Request.Key}";

    /// <inheritdoc/>
    protected override Task<bool> CanResume()
    {
        if (State.Request is null)
        {
            return Task.FromResult(false);
        }

        var observer = GrainFactory.GetGrain<IObserver>(Request.ObserverId, Request.ObserverKey);
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
