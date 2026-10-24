// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    IGrainTimer? _partitionReplayRetryTimer;
    bool _reconcilingPartitionReplays;
    bool _partitionReplayReconciliationStopped;

    bool CanRunPartitionReplayReconciliation => !IsRetired && !_removed && !_partitionReplayReconciliationStopped;
    bool CanReconcilePartitionReplays => !_reconcilingPartitionReplays && CanRunPartitionReplayReconciliation;
    bool FullReplayOwnsPartitions => State.IsReplaying || State.RunningState == ObserverRunningState.Replaying;
    bool CanProcessPartitionReplaySnapshot => !FullReplayOwnsPartitions && CanRunPartitionReplayReconciliation;
    bool HasPartitionReplayWork => State.ReplayingPartitions.Count > 0 || PartitionReplayCompletions.Any();

    async Task ReconcilePartitionReplayFences()
    {
        if (!CanReconcilePartitionReplays) return;
        _reconcilingPartitionReplays = true;
        var retry = false;
        try
        {
            retry = await ReconcilePartitionReplaySnapshot();
        }
        catch (Exception exception)
        {
            retry = true;
            logger.PartitionReplayReconciliationFailed(exception, _observerId);
        }
        finally
        {
            _reconcilingPartitionReplays = false;
            SchedulePartitionReplayRetry(retry || HasPartitionReplayWork);
        }
    }

    async Task<bool> ReconcilePartitionReplaySnapshot()
    {
        if (FullReplayOwnsPartitions) return false;
        var lookup = await GetPartitionReplayJobs();
        if (!CanProcessPartitionReplaySnapshot) return false;
        if (!lookup.TryGetResult(out var jobs)) return true;
        await RestoreMissingReplayFences(jobs);
        await ReconcileKnownPartitionReplays(jobs);
        return false;
    }

    async Task ReconcileKnownPartitionReplays(IEnumerable<JobState> jobs)
    {
        foreach (var partition in State.ReplayingPartitions.Concat(PartitionReplayCompletions.Select(record => record.Partition)).Distinct().ToArray())
        {
            if (!CanProcessPartitionReplaySnapshot) break;
            await ReconcilePartitionReplay(partition, jobs);
        }
    }

    async Task RestoreMissingReplayFences(IEnumerable<JobState> jobs)
    {
        foreach (var job in jobs.Where(job => !job.Status.HasFinished() && !HasRecordedReplayCompletion(job.Id)))
        {
            if (job.Request is not ReplayObserverPartitionRequest request) continue;
            if (request.ObserverKey != _observerKey) continue;
            if (State.ReplayingPartitions.Add(request.Key)) await WriteStateAsync();
        }
    }

    async Task ReconcilePartitionReplay(Key partition, IEnumerable<JobState> jobs)
    {
        if (jobs.Any(job => IsUnfinishedPartitionReplay(job, partition))) return;
        var pending = PartitionReplayCompletions.Where(record => record.Partition == partition && !record.HandedOff).ToArray();
        if (pending.Length == 0 && State.ReplayingPartitions.Contains(partition))
        {
            logger.RecoveringOrphanedPartitionReplay(_observerId, partition);
            var orphan = new PartitionReplayCompletionState(partition, null);
            await SavePartitionReplayCompletions([.. PartitionReplayCompletions, orphan]);
            pending = [orphan];
        }
        if (pending.Length > 0)
        {
            if (!await PrepareDrainedPartitionReplayHandoff(partition, pending)) return;
            await FinishPartitionReplayHandoff(partition);
        }
        await ForgetFinalizedPartitionReplays(partition);
    }

    async Task<bool> PrepareDrainedPartitionReplayHandoff(Key partition, PartitionReplayCompletionState[] pending)
    {
        if (pending.All(record => record.HandingOff)) return true;
        if (await HasUnfinishedPartitionCatchups(partition) || !CanProcessPartitionReplaySnapshot) return false;

        // No await separates the drain decision from prepare removing the marker; recovery excludes marked partitions.
        await PreparePartitionReplayHandoff(partition, pending);
        return true;
    }

    async Task<bool> HasUnfinishedPartitionCatchups(Key partition)
    {
        var jobStorage = storage.GetEventStore(_observerKey.EventStore).GetNamespace(_observerKey.Namespace).Jobs;
        var lookup = await jobStorage.GetJobs<ICatchUpObserverPartition, JobState>();
        if (!lookup.TryGetResult(out var jobs)) return true;

        // Use the shared terminal-status rule: stopped predecessors still own unfinished work.
        var predecessors = jobs.Where(job => IsRunningPartitionCatchup(job, partition)).ToArray();
        foreach (var job in predecessors.Where(job => job.Status == JobStatus.Stopped))
        {
            logger.WaitingForStoppedPartitionCatchup(_observerId, partition, job.Id);
        }
        return predecessors.Length > 0;
    }

    async Task ForgetFinalizedPartitionReplays(Key partition)
    {
        foreach (var record in PartitionReplayCompletions.Where(record => record.Partition == partition && record.HandedOff).ToArray())
        {
            if (!await HasReplayJobFinalized(record.JobId)) continue;
            await SavePartitionReplayCompletions(PartitionReplayCompletions.Where(candidate => candidate != record).ToArray());
        }
    }

    async Task PreparePartitionReplayHandoff(Key partition, PartitionReplayCompletionState[] pending)
    {
        var handled = pending.Select(record => record.LastHandledEventSequenceNumber).Aggregate(EventSequenceNumber.Unavailable, FurthestOf);
        var scanned = pending.Select(record => record.LastScannedEventSequenceNumber).Aggregate(EventSequenceNumber.Unavailable, FurthestOf);
        var types = pending.SelectMany(record => record.ReplayedEventTypes).Distinct().ToArray();
        await PreparePartitionReplayCompletion(partition, handled, types, scanned);
        await SavePartitionReplayCompletions(PartitionReplayCompletions.Select(record => record.Partition == partition && !record.HandedOff
            ? record with { HandingOff = true, LastHandledEventSequenceNumber = handled, LastScannedEventSequenceNumber = scanned }
            : record).ToArray());
    }

    async Task FinishPartitionReplayHandoff(Key partition)
    {
        if (!CanProcessPartitionReplaySnapshot) return;
        var pending = PartitionReplayCompletions.First(record => record.Partition == partition && !record.HandedOff);
        await WriteStateAsync();
        var completed = false;
        try
        {
            completed = await AdoptOrStartPartitionCatchup(partition, pending);
            if (!completed) return;
            await SavePartitionReplayCompletions(PartitionReplayCompletions.Where(record => record.Partition != partition || record.JobId is not null)
                .Select(record => record.Partition == partition ? record with { HandedOff = true } : record).ToArray());
            EvaluateSuspectedSkips();
        }
        finally
        {
            if (!completed) await RetainPartitionReplayHandoff(partition);
        }
    }

    async Task<bool> AdoptOrStartPartitionCatchup(Key partition, PartitionReplayCompletionState pending)
    {
        var jobStorage = storage.GetEventStore(_observerKey.EventStore).GetNamespace(_observerKey.Namespace).Jobs;
        var lookup = await jobStorage.GetJobs<ICatchUpObserverPartition, JobState>();
        if (!lookup.TryGetResult(out var jobs)) return false;
        State.ReplayingPartitions.Remove(partition);
        State.CatchingUpPartitions.Remove(partition);
        if (!jobs.Any(job => IsRunningPartitionCatchup(job, partition)))
            return await StartCatchupJobIfNeeded(partition, FurthestOf(pending.LastHandledEventSequenceNumber, pending.LastScannedEventSequenceNumber), fromStartWhenNothingRead: true);

        // Predecessors drained before prepare lifted the fence; a post-prepare job is safe to adopt, including after a crash.
        State.CatchingUpPartitions.Add(partition);
        Skips.JobStarted(partition);
        return true;
    }

    async Task RetainPartitionReplayHandoff(Key partition)
    {
        State.ReplayingPartitions.Add(partition);
        State.CatchingUpPartitions.Remove(partition);
        Skips.Release(partition, Skips.OwnershipTicket(partition));
        await WriteStateAsync();
    }

    void CancelPartitionReplayRetry()
    {
        var timer = _partitionReplayRetryTimer;
        _partitionReplayRetryTimer = null;
        timer?.Dispose();
    }

    void SchedulePartitionReplayRetry(bool needed)
    {
        CancelPartitionReplayRetry();
        if (!needed) return;
        if (!CanRunPartitionReplayReconciliation) return;
        _partitionReplayRetryTimer = this.RegisterGrainTimer(
            async _ =>
            {
                CancelPartitionReplayRetry();
                await ReconcilePartitionReplayFences();
            },
            new GrainTimerCreationOptions { DueTime = TimeSpan.FromSeconds(5), Period = Timeout.InfiniteTimeSpan, Interleave = false });
    }
}
