// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <summary>
    /// Recover from a crash by replaying every partition that still has an in-flight event marker.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// When the silo dies mid-handling — particularly during <c>AppendMany</c> across multiple partitions —
    /// the in-flight events storage retains a marker per partition that had work started but not acknowledged.
    /// On startup, this method spins up a per-partition catch-up job starting from the next event after the
    /// observer's last confirmed sequence number, so no events are silently skipped. Idempotency is preserved
    /// because the catch-up job dispatches events from a known good sequence number, and the markers are
    /// removed once handling completes successfully.
    /// </remarks>
    public async Task CatchUpInFlightPartitions()
    {
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);

        if (State.RunningState == ObserverRunningState.Quarantined)
        {
            return;
        }

        var inFlightStorage = GetInFlightStorage();
        var entries = (await inFlightStorage.GetFor(_observerId)).ToArray();
        if (entries.Length == 0)
        {
            return;
        }

        logger.RecoveringInFlightPartitions(entries.Length);

        var partitions = entries
            .Select(_ => _.Partition)
            .Distinct()
            .Where(p => !State.CatchingUpPartitions.Contains(p))
            .Where(p => !State.ReplayingPartitions.Contains(p))
            .Where(p => !Failures.IsFailed(p))
            .ToArray();

        var startFrom = State.LastHandledEventSequenceNumber.IsActualValue
            ? State.LastHandledEventSequenceNumber.Next()
            : EventSequenceNumber.First;

        foreach (var partition in partitions)
        {
            await EnqueuePartitionCatchUp(partition, startFrom);
        }
    }

    IInFlightEventsStorage GetInFlightStorage() =>
        storage
            .GetEventStore(_observerKey.EventStore)
            .GetNamespace(_observerKey.Namespace)
            .InFlightEvents;

    async Task EnqueuePartitionCatchUp(Key partition, EventSequenceNumber fromSequenceNumber)
    {
        logger.StartingInFlightCatchUpForPartition(partition, fromSequenceNumber);
        State.CatchingUpPartitions.Add(partition);
        await _jobsManager.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(
            new(_observerKey, Definition.Type, partition, fromSequenceNumber, Definition.EventTypes));
        await WriteStateAsync();
    }
}
