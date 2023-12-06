// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Kernel.Grains.Jobs;

namespace Aksio.Cratis.Kernel.Grains.Observation.Jobs;

/// <summary>
/// Represents a job for retrying a failed partition.
/// </summary>
public class ReplayObserverPartition : Job<ReplayObserverPartitionRequest, JobState<ReplayObserverPartitionRequest>>, IReplayObserverPartition
{
    /// <inheritdoc/>
    public override async Task OnCompleted()
    {
        if (State.Progress.SuccessfulSteps == 1)
        {
            var observer = GrainFactory.GetGrain<IObserver>(State.Request.ObserverId, State.Request.ObserverKey);
            await observer.PartitionReplayed(State.Request.Key);
        }
    }

    /// <inheritdoc/>
    protected override Task<IImmutableList<JobStepDetails>> PrepareSteps(ReplayObserverPartitionRequest request)
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
                    Events.EventObservationState.Replay,
                    request.EventTypes))
        }.ToImmutableList();

        return Task.FromResult<IImmutableList<JobStepDetails>>(steps);
    }
}
