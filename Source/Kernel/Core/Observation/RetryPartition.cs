// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Observation.Jobs;
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
[Cratis.Arc.Authorization.Authorize(Policy = Security.WellKnownAuthorizationPolicies.AdministratorOperations)]
[BelongsTo(WellKnownServices.Observers)]
public record RetryPartition(string EventStore, string Namespace, string ObserverId, string EventSequenceId, string Partition)
{
    /// <summary>
    /// Handles the command by accepting it as a job that invokes <see cref="IObserver.TryStartRecoverJobForFailedPartition"/> on the target observer grain.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get observer grains with.</param>
    /// <param name="storage">The storage containing observer definitions.</param>
    /// <returns><see cref="PartitionRecoveryOutcome.Accepted"/>; the job reports what became of it.</returns>
    /// <exception cref="ObserverOperatorCommandNotAccepted">The jobs manager did not accept the command.</exception>
    public async Task<PartitionRecoveryOutcome> Handle(IGrainFactory grainFactory, IStorage storage)
    {
        var eventSequenceId = await ObserverEventSequenceResolver.Resolve(storage, (EventStoreName)EventStore, (ObserverId)ObserverId, EventSequenceId);
        var key = new ObserverKey((ObserverId)ObserverId, (EventStoreName)EventStore, (EventStoreNamespaceName)Namespace, eventSequenceId);
        var jobId = await ObserverOperatorCommandJobs.Accept(grainFactory, key, ObserverOperatorCommand.RetryPartition, Partition);
        return jobId == JobId.NotSet
            ? throw new ObserverOperatorCommandNotAccepted(ObserverOperatorCommand.RetryPartition, key)
            : PartitionRecoveryOutcome.Accepted;
    }

    /// <summary>
    /// Requests operator recovery for a partition and rejects outcomes that start no recovery.
    /// </summary>
    /// <param name="observer">The observer whose partition should be retried.</param>
    /// <param name="key">The observer key used to identify a rejected recovery.</param>
    /// <param name="partition">The failed partition to retry.</param>
    /// <returns>The accepted recovery outcome.</returns>
    /// <exception cref="FailedPartitionRecoveryCouldNotStart">The partition recovery could not be started.</exception>
    internal static async Task<PartitionRecoveryOutcome> Start(IObserver observer, ObserverKey key, Key partition)
    {
        var outcome = await observer.TryStartRecoverJobForFailedPartition(partition);
        if (outcome != PartitionRecoveryOutcome.Started)
        {
            throw new FailedPartitionRecoveryCouldNotStart(partition, key);
        }

        return outcome;
    }
}
