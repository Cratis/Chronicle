// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the command for clearing all failed partition records for an observer.
/// </summary>
/// <param name="EventStore">The event store the observer belongs to.</param>
/// <param name="Namespace">The namespace within the event store the observer belongs to.</param>
/// <param name="ObserverId">The identifier of the observer.</param>
/// <param name="EventSequenceId">The event sequence the observer observes.</param>
/// <remarks>
/// Nothing else resets an individual failed partition back to a clean slate short of removing it entirely - a
/// quarantined partition stays quarantined until it is either retried enough times to recover or removed, and
/// nothing removes it but a genuine recovery or this command. This is the supported alternative to reaching into
/// storage directly to un-wedge an observer whose partitions cannot make progress on their own.
/// </remarks>
[Command]
[BelongsTo(WellKnownServices.Observers)]
public record ClearFailedPartitions(string EventStore, string Namespace, string ObserverId, string EventSequenceId)
{
    /// <summary>
    /// Handles the command by clearing all failed partition records on the target observer grain.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get observer grains with.</param>
    /// <returns>Awaitable task.</returns>
    public Task Handle(IGrainFactory grainFactory)
    {
        var eventSequenceId = string.IsNullOrEmpty(EventSequenceId) ? Concepts.EventSequences.EventSequenceId.Log : (Concepts.EventSequences.EventSequenceId)EventSequenceId;
        var key = new ObserverKey((ObserverId)ObserverId, (EventStoreName)EventStore, (EventStoreNamespaceName)Namespace, eventSequenceId);
        return grainFactory.GetGrain<IObserver>(key).ClearFailedPartitions();
    }
}
