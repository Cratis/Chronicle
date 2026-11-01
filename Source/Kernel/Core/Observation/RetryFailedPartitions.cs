// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the command for retrying every failed partition of an observer.
/// </summary>
/// <param name="EventStore">The event store the observer belongs to.</param>
/// <param name="Namespace">The namespace within the event store the observer belongs to.</param>
/// <param name="ObserverId">The identifier of the observer.</param>
/// <param name="EventSequenceId">The event sequence the observer observes; uses its stored sequence when empty, or the event log if unavailable.</param>
/// <remarks>
/// <para>
/// The operator's way of getting past partitions that failed together - a burst of timeouts while the observer was
/// busy, say - once the cause is gone. Each partition is retried by a job of its own, started a bounded batch at a
/// time, so retrying hundreds of them never asks the observer for more than it can answer. A job that concluded with
/// failed partitions is never reopened: a concluded reducer replay abandons its read model on any failure, so a retry
/// that reopened it would leave a half-built one in place.
/// </para>
/// <para>
/// Partitions the observer has quarantined are not retried - clear those first.
/// </para>
/// </remarks>
[Command]
[Cratis.Arc.Authorization.Authorize(Policy = Security.WellKnownAuthorizationPolicies.AdministratorOperations)]
[BelongsTo(WellKnownServices.Observers)]
public record RetryFailedPartitions(string EventStore, string Namespace, string ObserverId, string EventSequenceId)
{
    /// <summary>
    /// Handles the command by asking the target observer grain to recover its failed partitions.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get observer grains with.</param>
    /// <param name="storage">The storage containing observer definitions.</param>
    /// <returns>The <see cref="RetryFailedPartitionsResult"/> telling how many partitions were failed.</returns>
    public async Task<RetryFailedPartitionsResult> Handle(IGrainFactory grainFactory, IStorage storage)
    {
        var eventSequenceId = await ObserverEventSequenceResolver.Resolve(storage, (EventStoreName)EventStore, (ObserverId)ObserverId, EventSequenceId);
        var key = new ObserverKey((ObserverId)ObserverId, (EventStoreName)EventStore, (EventStoreNamespaceName)Namespace, eventSequenceId);
        var observer = grainFactory.GetGrain<IObserver>(key);
        var failedPartitions = (await observer.GetFailedPartitionKeys()).Count();
        await observer.TryRecoverAllFailedPartitions();
        return new RetryFailedPartitionsResult(failedPartitions);
    }
}
