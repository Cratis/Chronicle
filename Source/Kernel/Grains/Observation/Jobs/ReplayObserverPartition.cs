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
public class ReplayObserverPartition(ILogger<ReplayObserverPartition> logger) : Job<ReplayObserverPartitionRequest, JobStateWithLastHandledEvent>, IReplayObserverPartition
{
    /// <inheritdoc/>
    public override async Task OnCompleted()
    {
        using var scope = logger.BeginJobScope(JobId, JobKey);
        var observer = GrainFactory.GetGrain<IObserver>(Request.ObserverKey);
        if (State is { HandledAllEvents: false, LastHandledEventSequenceNumber.IsActualValue: true })
        {
            logger.NotAllEventsWereHandled(nameof(ReplayObserverPartition), State.LastHandledEventSequenceNumber);
        }

        if (!State.LastHandledEventSequenceNumber.IsActualValue)
        {
            logger.NoEventsWereHandled(nameof(ReplayObserverPartition));
            return;
        }
        await observer.PartitionReplayed(Request.Key, State.LastHandledEventSequenceNumber);
    }

    /// <inheritdoc/>
    protected override Task OnStepCompleted(JobStepId jobStepId, JobStepResult result)
    {
        State.HandleResult(result);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override Task<IImmutableList<JobStepDetails>> PrepareSteps(ReplayObserverPartitionRequest request)
    {
        var steps = new[]
        {
            CreateStep<IHandleEventsForPartition>(
                new HandleEventsForPartitionArguments(
                    request.ObserverKey,
                    request.ObserverSubscription,
                    request.Key,
                    request.FromSequenceNumber,
                    request.ToSequenceNumber,
                    EventObservationState.Replay,
                    request.EventTypes))
        }.ToImmutableList();

        return Task.FromResult<IImmutableList<JobStepDetails>>(steps);
    }
}
