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

    /// <summary>
    /// Releases the replay marker a replay request added when its job could not be started.
    /// </summary>
    /// <remarks>
    /// When no job owns a marker this call added, the untouched partition goes back to catch-up from the captured bound,
    /// as a refused replay does; the release is idempotent with the refusal notification a refused job sends itself.
    /// A marker another replay already held stays with that replay. If a replay job for the partition is known to be
    /// still unfinished, the marker stays with it and reconciliation hands it off once the job concludes.
    /// </remarks>
    /// <param name="partition">The partition the replay was requested for.</param>
    /// <param name="heldBackFrom">The first event held back from live delivery for the replay.</param>
    /// <param name="addedFence">Whether this request added the marker.</param>
    /// <returns>Awaitable task.</returns>
    async Task ReleaseUnstartedPartitionReplay(Key partition, EventSequenceNumber heldBackFrom, bool addedFence)
    {
        if (!addedFence) return;
        var lookup = await GetPartitionReplayJobs();
        if (lookup.TryGetResult(out var jobs) && jobs.Any(job => IsUnfinishedPartitionReplay(job, partition)))
        {
            await ReconcilePartitionReplayFences();
            return;
        }

        await ReleaseRefusedPartitionReplay(partition, heldBackFrom);
    }
}
