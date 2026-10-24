// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using JobError = Cratis.Orleans.Storage.Jobs.JobError;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    IEnumerable<PartitionReplayCompletionState> PartitionReplayCompletions => State.PartitionReplayCompletions ?? [];

    /// <inheritdoc/>
    public async Task<bool> IsPartitionFenced(Key partition, JobId ownJob) =>
        State.ReplayingPartitions.Contains(partition) || await HasUnfinishedPartitionReplay(partition, ownJob);

    /// <inheritdoc/>
    public Task PartitionReplayed(Key partition, EventSequenceNumber lastHandledEventSequenceNumber, EventSequenceNumber lastScannedEventSequenceNumber, EventType[] replayedEventTypes, JobId jobId) =>
        RecordPartitionReplayCompletion(new(partition, jobId)
        {
            LastHandledEventSequenceNumber = lastHandledEventSequenceNumber,
            LastScannedEventSequenceNumber = lastScannedEventSequenceNumber,
            ReplayedEventTypes = replayedEventTypes
        });

    async Task RecordPartitionReplayCompletion(PartitionReplayCompletionState completion)
    {
        if (IsRetired || _removed) return;
        if (!PartitionReplayCompletions.Any(record => record.Partition == completion.Partition && record.JobId == completion.JobId))
        {
            State = State with { PartitionReplayCompletions = [.. PartitionReplayCompletions, completion] };
        }
        else if (completion.JobId is null)
        {
            State = State with
            {
                PartitionReplayCompletions = PartitionReplayCompletions.Select(record => record.Partition == completion.Partition && record.JobId is null
                    ? record with
                    {
                        LastHandledEventSequenceNumber = FurthestOf(record.LastHandledEventSequenceNumber, completion.LastHandledEventSequenceNumber),
                        LastScannedEventSequenceNumber = FurthestOf(record.LastScannedEventSequenceNumber, completion.LastScannedEventSequenceNumber),
                        ReplayedEventTypes = record.ReplayedEventTypes.Concat(completion.ReplayedEventTypes).Distinct().ToArray(),
                        HandingOff = false
                    }
                    : record).ToArray()
            };
        }
        await WriteStateAsync();
        await ReconcilePartitionReplayFences();
    }

    async Task<bool> HasUnfinishedPartitionReplay(Key partition, JobId? ownJob = null)
    {
        var lookup = await GetPartitionReplayJobs();
        return !lookup.TryGetResult(out var jobs) || jobs.Any(job => IsUnfinishedPartitionReplay(job, partition, ownJob));
    }

    bool IsUnfinishedPartitionReplay(JobState job, Key partition, JobId? ownJob = null) =>
        job.Id != ownJob && !HasRecordedReplayCompletion(job.Id) && !job.Status.HasFinished() &&
        job.Request is ReplayObserverPartitionRequest request && request.ObserverKey == _observerKey && request.Key == partition;

    bool HasRecordedReplayCompletion(JobId jobId) => PartitionReplayCompletions.Any(record => record.JobId == jobId);

    bool IsRunningPartitionCatchup(JobState job, Key partition) =>
        !job.Status.HasFinished() && job.Request is CatchUpObserverPartitionRequest request && request.ObserverKey == _observerKey && request.Key == partition;

    async Task<bool> HasReplayJobFinalized(JobId? jobId)
    {
        if (jobId is null) return true;
        var jobStorage = storage.GetEventStore(_observerKey.EventStore).GetNamespace(_observerKey.Namespace).Jobs;
        var lookup = await jobStorage.GetJob(jobId);
        return (lookup.TryGetResult(out var job) && job.Status.HasFinished()) ||
            (lookup.TryGetError(out var error) && error == JobError.NotFound);
    }

    async Task SavePartitionReplayCompletions(IEnumerable<PartitionReplayCompletionState> records)
    {
        var previous = State.PartitionReplayCompletions;
        State = State with { PartitionReplayCompletions = records.ToArray() };
        try
        {
            await WriteStateAsync();
        }
        catch
        {
            State = State with { PartitionReplayCompletions = previous };
            throw;
        }
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
