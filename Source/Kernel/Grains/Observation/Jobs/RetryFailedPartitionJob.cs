// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Jobs;

namespace Aksio.Cratis.Kernel.Grains.Observation.Jobs;

/// <summary>
/// Represents a job for retrying a failed partition.
/// </summary>
public class RetryFailedPartitionJob : Job<RetryFailedPartitionRequest, JobState<RetryFailedPartitionRequest>>, IRetryFailedPartitionJob
{
    RetryFailedPartitionRequest? _request;

    /// <inheritdoc/>
    public override async Task OnCompleted()
    {
        if (_request == null) return;
        var observer = GrainFactory.GetGrain<IObserver>(_request.ObserverId, _request.ObserverKey);

        if (State.Progress.SuccessfulSteps == 1)
        {
        }
    }

    /// <inheritdoc/>
    protected override async Task StartJob(RetryFailedPartitionRequest request)
    {
        _request = request;

        await AddStep<IHandleEventsForPartition, HandleEventsForPartitionArguments>(
            new HandleEventsForPartitionArguments(
                request.ObserverId,
                request.ObserverKey,
                request.ObserverSubscription,
                request.Key,
                request.FromSequenceNumber,
                request.EventTypes));
    }
}
