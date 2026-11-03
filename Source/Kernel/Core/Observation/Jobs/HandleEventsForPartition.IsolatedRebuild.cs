// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// The isolated rebuild mode of <see cref="HandleEventsForPartition"/>.
/// </summary>
/// <remarks>
/// An isolated rebuild delivers to an <see cref="IProjectionPartitionRebuilder"/> instead of the projection's
/// subscriber, so the partition is rebuilt from empty out of sight of the live read model and published in one
/// replace at the end. Every run starts again from the first event: nothing is checkpointed, because the rebuild
/// held in memory does not survive the run, and the result reports nothing handled until the publish succeeds.
/// </remarks>
public partial class HandleEventsForPartition
{
    /// <summary>
    /// The reason given when a newer write superseded the rebuild, so the step is run again.
    /// </summary>
    internal const string IsolatedRebuildSuperseded = "Isolated partition rebuild was superseded by a newer write";

    bool _isolatedRebuild;
    EventSequenceNumber _isolatedRangeEnd = EventSequenceNumber.Unavailable;

    static EventSequenceNumber FurthestOf(EventSequenceNumber lastHandled, EventSequenceNumber lastScanned)
    {
        if (!lastScanned.IsActualValue)
        {
            return lastHandled;
        }

        return !lastHandled.IsActualValue || lastScanned > lastHandled ? lastScanned : lastHandled;
    }

    IObserverSubscriber? ResolveSubscriber(ObserverSubscription subscription, Key partition, bool isolatedRebuild)
    {
        // Resolve the delivery target even when rebuilding in isolation; the subscriber context is built from it.
        var key = GetObserverSubscriberKey(subscription, partition);
        _isolatedRebuild = isolatedRebuild;
        return isolatedRebuild
            ? GrainFactory.GetGrain<IProjectionPartitionRebuilder>(JobStepId.Value.ToString())
            : GrainFactory.GetGrain(subscription.SubscriberType, key) as IObserverSubscriber;
    }

    async Task<EventSequenceNumber> BeginIsolatedRebuild(HandleEventsForPartitionState currentState, EventSequenceNumber lastSuccessfullyHandled)
    {
        if (!_isolatedRebuild)
        {
            return lastSuccessfullyHandled;
        }

        await ((IProjectionPartitionRebuilder)_subscriber!).Begin(currentState.ObserverKey, currentState.Partition);
        return EventSequenceNumber.Unavailable;
    }

    async Task<JobStepResult> CompleteRange(HandleEventsForPartitionState currentState, EventSequenceNumber lastHandled, EventSequenceNumber lastScanned)
    {
        if (!_isolatedRebuild)
        {
            return JobStepResult.Succeeded(CreateResult(lastHandled) with { LastScannedEventSequenceNumber = lastScanned });
        }

        var watermark = FurthestOf(lastHandled, lastScanned);
        if (!watermark.IsActualValue)
        {
            // Empty partitions still need to replace stale state, but only through the finite range
            // captured before reading. A sentinel would permit deleting a newer live write.
            watermark = _isolatedRangeEnd;
        }

        if (await ((IProjectionPartitionRebuilder)_subscriber!).Publish(watermark))
        {
            return JobStepResult.Succeeded(new HandleEventsForPartitionResult(lastHandled) { LastScannedEventSequenceNumber = lastScanned });
        }

        // The job runs a transient failure again, which rebuilds from empty against the newer write. Should the
        // attempts run out, the partition fails at the start of the range: the live read model never saw the rebuild.
        logger.IsolatedRebuildSuperseded(currentState.Partition, watermark);
        return JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(
            new HandleEventsForPartitionResult(EventSequenceNumber.Unavailable)
            {
                TransientFailure = new(currentState.Partition, currentState.StartEventSequenceNumber, [IsolatedRebuildSuperseded])
            },
            [IsolatedRebuildSuperseded],
            string.Empty));
    }
}
