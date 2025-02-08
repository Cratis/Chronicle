// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Grains.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Observation.Jobs;

/// <summary>
/// Represents a job for retrying a failed partition.
/// </summary>
/// <param name="logger">The logger.</param>
public class RetryFailedPartitionJob(ILogger<RetryFailedPartitionJob> logger) : Job<RetryFailedPartitionRequest, JobStateWithLastHandledEvent>, IRetryFailedPartitionJob
{
    /// <inheritdoc/>
    public override async Task OnCompleted()
    {
        using var scope = logger.BeginJobScope(JobId, JobKey);
        var observer = GrainFactory.GetGrain<IObserver>(Request.ObserverKey);

        if (State is { HandledAllEvents: false, LastHandledEventSequenceNumber.IsActualValue: true })
        {
            logger.NotAllEventsWereHandled(nameof(RetryFailedPartitionJob), State.LastHandledEventSequenceNumber);
            await observer.FailedPartitionPartiallyRecovered(Request.Key, State.LastHandledEventSequenceNumber);
        }

        if (!State.LastHandledEventSequenceNumber.IsActualValue)
        {
            logger.NoEventsWereHandled(nameof(RetryFailedPartitionJob));
            return;
        }
        await observer.FailedPartitionRecovered(Request.Key, State.LastHandledEventSequenceNumber);
    }

    /// <inheritdoc/>
    protected override JobDetails GetJobDetails() => $"{Request.ObserverKey.ObserverId}-{Request.Key}";

    /// <inheritdoc/>
    protected override Task<bool> CanResume()
    {
        if (State.Request is null)
        {
            return Task.FromResult(false);
        }

        var observer = GrainFactory.GetGrain<IObserver>(Request.ObserverKey);
        return observer.IsSubscribed();
    }

    /// <inheritdoc/>
    protected override Task OnStepCompleted(JobStepId jobStepId, JobStepResult result)
    {
        State.HandleResult(result);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override Task<IImmutableList<JobStepDetails>> PrepareSteps(RetryFailedPartitionRequest request)
    {
        var steps = new[]
        {
            CreateStep<IHandleEventsForPartition>(
                new HandleEventsForPartitionArguments(
                    request.ObserverKey,
                    request.ObserverSubscription,
                    request.Key,
                    request.FromSequenceNumber,
                    EventSequenceNumber.Max,
                    EventObservationState.None,
                    request.EventTypes))
        }.ToImmutableList();

        return Task.FromResult<IImmutableList<JobStepDetails>>(steps);
    }
}
