// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the command for retrying the recovery of a failed partition in an observer.
/// </summary>
/// <param name="EventStore">The name of the event store.</param>
/// <param name="Namespace">The namespace within the event store.</param>
/// <param name="ObserverId">The unique identifier of the observer.</param>
/// <param name="EventSequenceId">The identifier of the event sequence; uses the stored observer sequence when empty, or the event log if unavailable.</param>
/// <param name="Partition">The partition identifier to retry.</param>
[Command]
[BelongsTo(WellKnownServices.Observers)]
public record RetryPartition(string EventStore, string Namespace, string ObserverId, string EventSequenceId, string Partition)
{
    /// <summary>
    /// Handles the command by invoking <see cref="IObserver.TryStartRecoverJobForFailedPartition"/> on the target observer grain.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get observer grains with.</param>
    /// <param name="storage">The storage containing observer definitions.</param>
    /// <returns>The <see cref="PartitionRecoveryOutcome"/> describing what happened.</returns>
    public async Task<PartitionRecoveryOutcome> Handle(IGrainFactory grainFactory, IStorage storage)
    {
        var eventSequenceId = await ObserverEventSequenceResolver.Resolve(storage, (EventStoreName)EventStore, (ObserverId)ObserverId, EventSequenceId);
        var key = new ObserverKey((ObserverId)ObserverId, (EventStoreName)EventStore, (EventStoreNamespaceName)Namespace, eventSequenceId);
        return await grainFactory.GetGrain<IObserver>(key).TryStartRecoverJobForFailedPartition(Partition);
    }
}
