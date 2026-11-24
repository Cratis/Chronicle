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
/// Represents the command for retrying every failed partition of an observer.
/// </summary>
/// <param name="EventStore">The event store the observer belongs to.</param>
/// <param name="Namespace">The namespace within the event store the observer belongs to.</param>
/// <param name="ObserverId">The identifier of the observer.</param>
/// <param name="EventSequenceId">The event sequence the observer observes; uses its stored sequence when empty, or the event log if unavailable.</param>
/// <remarks>
/// <para>
/// The operator's way of getting past partitions that failed together - a burst of timeouts while the observer was
/// busy, say - once the cause is gone. Each partition is retried by a job of its own, requested in a separate grain
/// call so the observer can serve other requests between retries. A job that concluded with
/// failed partitions is never reopened: a concluded reducer replay abandons its read model on any failure, so a retry
/// that reopened it would leave a half-built one in place.
/// </para>
/// <para>
/// Exhausting the automatic attempt limit does not prevent an operator retry. Other quarantine policies still apply.
/// </para>
/// </remarks>
[Command]
[Cratis.Arc.Authorization.Authorize(Policy = Security.WellKnownAuthorizationPolicies.AdministratorOperations)]
[BelongsTo(WellKnownServices.Observers)]
public record RetryFailedPartitions(string EventStore, string Namespace, string ObserverId, string EventSequenceId)
{
    /// <summary>
    /// Handles the command by accepting it as a job that asks the target observer grain to recover its failed partitions.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get observer grains with.</param>
    /// <param name="storage">The storage containing observer definitions.</param>
    /// <returns>The <see cref="RetryFailedPartitionsResult"/> telling how many partitions were failed.</returns>
    /// <exception cref="FailedPartitionsRecoveryCouldNotStart">No failed partition recovery could be started.</exception>
    public async Task<RetryFailedPartitionsResult> Handle(IGrainFactory grainFactory, IStorage storage)
    {
        var eventSequenceId = await ObserverEventSequenceResolver.Resolve(storage, (EventStoreName)EventStore, (ObserverId)ObserverId, EventSequenceId);
        var key = new ObserverKey((ObserverId)ObserverId, (EventStoreName)EventStore, (EventStoreNamespaceName)Namespace, eventSequenceId);
        var (failedPartitions, jobId) = await Accept(grainFactory, storage, key);
        if (jobId == JobId.NotSet)
        {
            throw new FailedPartitionsRecoveryCouldNotStart(key);
        }

        return new RetryFailedPartitionsResult(failedPartitions);
    }

    /// <summary>
    /// Accepts the retry of every failed partition of an observer as a job.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get the jobs manager with.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the failed partitions.</param>
    /// <param name="key">The <see cref="ObserverKey"/> of the observer.</param>
    /// <returns>How many partitions are failed, and the <see cref="JobId"/> of the job retrying them; <see cref="JobId.NotSet"/> if none is failed or no job could be started.</returns>
    public static async Task<(int FailedPartitions, JobId JobId)> Accept(IGrainFactory grainFactory, IStorage storage, ObserverKey key)
    {
        var failedPartitions = await FailedPartitionCount(storage, key);
        return (failedPartitions, failedPartitions == 0
            ? JobId.NotSet
            : await ObserverOperatorCommandJobs.Accept(grainFactory, key, ObserverOperatorCommand.RetryFailedPartitions));
    }

    /// <summary>
    /// Gets how many partitions of an observer are failed, from storage so the observer grain is not asked.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> holding the failed partitions.</param>
    /// <param name="key">The <see cref="ObserverKey"/> of the observer.</param>
    /// <returns>The number of failed partitions.</returns>
    internal static async Task<int> FailedPartitionCount(IStorage storage, ObserverKey key) =>
        (await storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace).FailedPartitions.GetFor(key.ObserverId)).Partitions.Count();

    /// <summary>
    /// Requests operator recovery for every partition, continuing past partitions that cannot start.
    /// </summary>
    /// <param name="observer">The observer whose partitions should be retried.</param>
    /// <param name="partitions">The failed partitions to retry.</param>
    /// <returns>The number of partitions whose recovery was accepted.</returns>
    internal static async Task<int> Start(IObserver observer, IEnumerable<Key> partitions)
    {
        var started = 0;
        foreach (var partition in partitions)
        {
            if (await TryStart(observer, partition))
            {
                started++;
            }
        }

        return started;
    }

    static async Task<bool> TryStart(IObserver observer, Key partition)
    {
        try
        {
            return await observer.TryStartRecoverJobForFailedPartition(partition) == PartitionRecoveryOutcome.Started;
        }
        catch (FailedPartitionRecoveryCouldNotStart)
        {
            return false;
        }
    }
}
