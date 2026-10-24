// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using JobError = Cratis.Orleans.Storage.Jobs.JobError;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <summary>
    /// Completion is reported before the jobs manager persists the job's terminal status.
    /// </summary>
    readonly HashSet<JobId> _concludedPartitionReplayJobs = [];

    /// <inheritdoc/>
    public async Task<bool> IsPartitionFenced(Key partition, JobId ownJob) =>
        State.ReplayingPartitions.Contains(partition) || await HasUnfinishedPartitionReplay(partition, ownJob);

    /// <inheritdoc/>
    public async Task PartitionReplayed(Key partition, EventSequenceNumber lastHandledEventSequenceNumber, EventSequenceNumber lastScannedEventSequenceNumber, EventType[] replayedEventTypes, JobId jobId)
    {
        if (IsRetired || _removed) return;
        _concludedPartitionReplayJobs.Add(jobId);
        await CompleteUnfencedPartitionReplay(partition, lastHandledEventSequenceNumber, lastScannedEventSequenceNumber, replayedEventTypes);
    }

    async Task CompleteUnfencedPartitionReplay(Key partition, EventSequenceNumber lastHandledEventSequenceNumber, EventSequenceNumber lastScannedEventSequenceNumber, EventType[] replayedEventTypes)
    {
        if (await HasUnfinishedPartitionReplay(partition)) return;
        await CompletePartitionReplay(partition, lastHandledEventSequenceNumber, replayedEventTypes, lastScannedEventSequenceNumber);
    }

    async Task<bool> HasUnfinishedPartitionReplay(Key partition, JobId? ownJob = null)
    {
        var lookup = await GetPartitionReplayJobs();
        return !lookup.TryGetResult(out var jobs) || jobs.Any(job => IsUnfinishedPartitionReplay(job, partition, ownJob));
    }

    bool IsUnfinishedPartitionReplay(JobState job, Key partition, JobId? ownJob = null) =>
        job.Id != ownJob && !_concludedPartitionReplayJobs.Contains(job.Id) && !job.Status.HasFinished() &&
        job.Request is ReplayObserverPartitionRequest request && request.ObserverKey == _observerKey && request.Key == partition;

    async Task ReconcilePartitionReplayFences()
    {
        if (State.ReplayingPartitions.Count == 0) return;
        var lookup = await GetPartitionReplayJobs();
        if (!lookup.TryGetResult(out var jobs)) return;
        var orphaned = State.ReplayingPartitions.Where(partition => !jobs.Any(job => IsUnfinishedPartitionReplay(job, partition))).ToArray();
        if (orphaned.Length == 0) return;
        foreach (var partition in orphaned) State.ReplayingPartitions.Remove(partition);
        await WriteStateAsync();
    }

    async Task<Catch<IImmutableList<JobState>, JobError>> GetPartitionReplayJobs()
    {
        try
        {
            var jobStorage = storage.GetEventStore(_observerKey.EventStore).GetNamespace(_observerKey.Namespace).Jobs;
            return await jobStorage.GetJobs<IReplayObserverPartition, JobState>();
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    async Task RollBackPartitionReplayFence(Key partition, bool addedFence)
    {
        if (!addedFence) return;
        State.ReplayingPartitions.Remove(partition);
        await WriteStateAsync();
    }
}
