// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

/// <content>Persists partition repair intent and reconciles uncertain job admissions.</content>
public partial class Observer
{
    readonly Dictionary<Key, PartitionRepairState> _partitionRepairs = [];
    readonly HashSet<JobId> _issuedPartitionRepairs = [];
    readonly Dictionary<JobId, Task<JobDrainStatus>> _partitionRepairDrains = [];
    readonly HashSet<Key> _partitionRepairsInProgress = [];

    static bool SameCoverage(PartitionRepairCoverage left, PartitionRepairCoverage right) =>
        left.Through == right.Through && left.RetainOtherCounts == right.RetainOtherCounts && left.EventTypes.SequenceEqual(right.EventTypes);

    [LoggerMessage(LogLevel.Warning, "Could not reconcile durable partition repair for {Partition}; the repair remains owed")]
    static partial void PartitionRepairRecoveryFailed(ILogger logger, Key partition, Exception exception);

    void LoadPartitionRepairs()
    {
        foreach (var repair in State.PartitionRepairs ?? [])
        {
            _partitionRepairs.Add(repair.Partition, repair);
        }
        PreservePartitionRepairHolds();
    }

    async Task RestorePartitionRepairs()
    {
        if (!CanGrantClaims() || !_partitionRepairs.Values.Any(repair => repair.Active is not null)) return;

        foreach (var repair in _partitionRepairs.Values.ToArray())
        {
            if (repair.Active is not null) RetirePartitionRepair(repair);
        }
        await PersistPartitionRepairs();
    }

    void PreservePartitionRepairHolds() => State.ReplayingPartitions.UnionWith(_partitionRepairs.Keys);

    bool OwnsPartitionRepairRecovery(IObserverJobRequest request) =>
        request is ReplayObserverPartitionRequest replay && _partitionRepairs.TryGetValue(replay.Key, out var repair) && !repair.AwaitingLegacyReplay &&
        (repair.Active is not { } active || !_issuedPartitionRepairs.Contains(active.Job) || replay.OwnershipEpoch != active.Epoch.Value);

    async Task EnqueuePartitionRepair(Key partition, PartitionRepairCoverage coverage)
    {
        if (_partitionRepairs.TryGetValue(partition, out var repair))
        {
            var matchesUnissuedHead = repair is { Active: null, CatchUpFrom: null } && SameCoverage(repair.Head, coverage);
            if (!matchesUnissuedHead && !repair.Waiting.Any(waiting => SameCoverage(waiting, coverage)))
            {
                _partitionRepairs[partition] = repair with { Waiting = [.. repair.Waiting, coverage] };
            }
            await PersistPartitionRepairs();
            return;
        }

        var legacy = State.ReplayingPartitions.Contains(partition);
        _partitionRepairs.Add(partition, new(partition, coverage, State.NextEventSequenceNumber, null, null, [], AwaitingLegacyReplay: legacy));
        HoldPartitionForRepair(partition);
        if (!legacy) GrantPartitionRepairClaim(partition, null, State.NextEventSequenceNumber);
        await PersistPartitionRepairs();
        if (!legacy) await ProgressPartitionRepair(partition, requested: true);
    }

    void HoldPartitionForRepair(Key partition)
    {
        State.ReplayingPartitions.Add(partition);
        _partitionHoldsVersion++;
        Skips.JobStarted(partition);
        RecordedHandlingState();
    }

    void GrantPartitionRepairClaim(Key partition, JobId? job, EventSequenceNumber position) =>
        GuardShadowClaims(() => ReportShadowDivergences(_shadowClaims.Grant(partition, ClaimHolderKind.PartitionReplay, job, position)));

    async Task PersistPartitionRepairs()
    {
        var writes = _persistedStateWrites;
        await WriteStateAsync();
        if (writes == _persistedStateWrites || !CanGrantClaims())
        {
            throw new InvalidOperationException("The observer could not persist its partition repair.");
        }
    }

    bool CanRunPartitionRepairs(bool requested = false) =>
        CanGrantClaims() && Definition.IsReplayable && !IsWithoutClient && !IsQuarantined && !State.IsReplaying &&
        State.RunningState != ObserverRunningState.Replaying && (requested || _subscription.IsSubscribed);

    async Task RecoverPartitionRepairs()
    {
        if (!CanRunPartitionRepairs()) return;

        foreach (var partition in _partitionRepairs.Keys.ToArray())
        {
            try
            {
                await ProgressPartitionRepair(partition);
            }
            catch (Exception exception)
            {
                PartitionRepairRecoveryFailed(logger, partition, exception);
            }
        }
    }

    async Task ProgressPartitionRepair(Key partition, bool requested = false)
    {
        if (!CanRunPartitionRepairs(requested) || !_partitionRepairsInProgress.Add(partition)) return;

        using var start = BeginPartitionJobStart();
        try
        {
            if (!_partitionRepairs.TryGetValue(partition, out var repair) || repair.AwaitingLegacyReplay) return;
            if (repair.Active is { } active)
            {
                if (IsCurrentPartitionRepair(partition, active)) return;
                repair = RetirePartitionRepair(repair);
            }
            if (repair.Retiring is { } retiring && !await DrainPartitionRepair(partition, retiring)) return;
            if (!CanRunPartitionRepairs(requested)) return;

            repair = _partitionRepairs[partition];
            if (repair.CatchUpFrom is { } position && repair.Waiting.Length == 0)
            {
                await FinishPartitionRepair(repair, position);
                return;
            }
            if (repair.CatchUpFrom is { } catchUpFrom) repair = NextPartitionRepair(repair, catchUpFrom);
            await DispatchPartitionRepair(repair, requested);
        }
        finally
        {
            _partitionRepairsInProgress.Remove(partition);
        }
    }

    PartitionRepairState RetirePartitionRepair(PartitionRepairState repair)
    {
        // The next persisted epoch denies future admission of this exact old id. A Start that already passed
        // admission still owns its grain turn, so the fence is never itself evidence that draining is unnecessary.
        GrantPartitionRepairClaim(repair.Partition, null, repair.HeldBackFrom);
        var fence = _shadowClaims.ClaimFor(repair.Partition);
        if (repair.Active is not { } active || fence is null || fence.Epoch.Value <= active.Epoch.Value || fence.Job is not null)
        {
            throw new InvalidOperationException("The observer could not fence the previous partition repair.");
        }
        _issuedPartitionRepairs.Remove(active.Job);
        var waiting = repair.Waiting;
        if (waiting.FirstOrDefault() is { } next && SameCoverage(repair.Head, next)) waiting = waiting[1..];
        var retired = repair with { Retiring = repair.Active, Active = null, Waiting = waiting };
        _partitionRepairs[repair.Partition] = retired;
        return retired;
    }

    async Task<bool> DrainPartitionRepair(Key partition, PartitionRepairGrant retiring)
    {
        if (!_partitionRepairDrains.TryGetValue(retiring.Job, out var pending))
        {
            await PersistPartitionRepairs();
            var key = new JobKey(_observerKey.EventStore.Value, _observerKey.Namespace.Value);
            var job = GrainFactory.GetGrain<IReplayObserverPartition>(retiring.Job, key);
            var drain = job as IJobDrain ?? job.AsReference<IJobDrain>();
            pending = drain.StopAndDrain(TimeSpan.FromSeconds(1));
            _partitionRepairDrains.Add(retiring.Job, pending);
        }

        // Leave the observer turn while Start/End or step reporting might still be calling back to us.
        if (!pending.IsCompleted) return false;
        _partitionRepairDrains.Remove(retiring.Job);
        if ((await pending) is not (JobDrainStatus.Drained or JobDrainStatus.Completed)) return false;

        var repair = _partitionRepairs[partition];
        _partitionRepairs[partition] = repair with { Retiring = null };
        await PersistPartitionRepairs();
        return true;
    }

    async Task DispatchPartitionRepair(PartitionRepairState repair, bool requested)
    {
        var partition = repair.Partition;
        HoldPartitionForRepair(partition);
        var checkpoint = _continuationLifetime.Capture();
        if (!repair.CountsPrepared)
        {
            await checkpoint.Await(() => ResetPartitionReplayCounts(partition, repair.Head));
        }
        if (!CanRunPartitionRepairs(requested)) return;

        var job = JobId.New();
        GrantPartitionRepairClaim(partition, job, repair.HeldBackFrom);
        var claim = _shadowClaims.ClaimFor(partition);
        if (claim is null || claim.Job != job) throw new InvalidOperationException("The observer could not grant its partition repair.");

        var grant = new PartitionRepairGrant(job, claim.Epoch);
        _partitionRepairs[partition] = _partitionRepairs[partition] with { Active = grant, CountsPrepared = true };
        await checkpoint.Await(PersistPartitionRepairs);
        if (!CanRunPartitionRepairs(requested) ||
            !State.ReplayingPartitions.Contains(partition) ||
            _shadowClaims.ClaimFor(partition) != claim ||
            _partitionRepairs[partition].Active != grant) return;

        _issuedPartitionRepairs.Add(job);
        try
        {
            var (through, retainOtherCounts, eventTypes) = repair.Head;
            var result = await _jobsManager.Start<IReplayObserverPartition, ReplayObserverPartitionRequest>(job, new(_observerKey, Definition.Type, partition, EventSequenceNumber.First, through, eventTypes)
            {
                ReplaysAllEventTypes = !retainOtherCounts,
                ReaderScope = ReaderScope,
                HeldBackFrom = repair.HeldBackFrom,
                OwnershipEpoch = grant.Epoch.Value
            });
            if (result.TryGetResult(out var started) && started == job) return;
            result.TryGetError(out var error);

            throw new InvalidOperationException($"Partition repair {job} did not acknowledge startup: {error}.");
        }
        catch
        {
            // A refused or missing reply does not prove there is no writer. The next tick fences and drains this id.
            _issuedPartitionRepairs.Remove(job);
            throw;
        }
    }

    bool AcceptsPartitionRepairReport(Key partition, ulong? epoch)
    {
        if (!State.ReplayingPartitions.Contains(partition)) return false;
        var claim = _shadowClaims.ClaimFor(partition);
        if (!_partitionRepairs.TryGetValue(partition, out var repair) || repair.AwaitingLegacyReplay)
        {
            return epoch is null || claim is { Holder: ClaimHolderKind.PartitionReplay } && claim.Epoch.Value == epoch;
        }

        // The existing wire notification carries the epoch, which is unique to the persisted (partition, job) grant.
        return repair.Active is { } active && active.Epoch.Value == epoch && IsCurrentPartitionRepair(partition, active);
    }

    bool IsCurrentPartitionRepair(Key partition, PartitionRepairGrant grant) =>
        _issuedPartitionRepairs.Contains(grant.Job) &&
        _shadowClaims.ClaimFor(partition) is { Holder: ClaimHolderKind.PartitionReplay } claim &&
        claim.Job == grant.Job && claim.Epoch == grant.Epoch;

    async Task<bool> AdvancePartitionRepair(Key partition, EventSequenceNumber nextPosition, bool refused = false)
    {
        if (!_partitionRepairs.TryGetValue(partition, out var repair)) return false;
        if (repair.Active is { } active) _issuedPartitionRepairs.Remove(active.Job);
        if (refused && !repair.AwaitingLegacyReplay) nextPosition = repair.HeldBackFrom;

        _partitionRepairs[partition] = repair.AwaitingLegacyReplay
            ? repair with { AwaitingLegacyReplay = false, HeldBackFrom = nextPosition }
            : repair.Waiting.Length > 0
                ? NextPartitionRepair(repair, nextPosition)
                : repair with { Active = null, CatchUpFrom = nextPosition };
        await PersistPartitionRepairs();
        await ProgressPartitionRepair(partition, requested: true);
        return true;
    }

    PartitionRepairState NextPartitionRepair(PartitionRepairState repair, EventSequenceNumber position)
    {
        var next = new PartitionRepairState(repair.Partition, repair.Waiting[0], position, null, null, repair.Waiting[1..]);
        _partitionRepairs[repair.Partition] = next;
        return next;
    }

    async Task FinishPartitionRepair(PartitionRepairState repair, EventSequenceNumber position)
    {
        var partition = repair.Partition;
        var ownership = Skips.OwnershipTicket(partition);
        await ProbeAndStartCatchupJobFrom(partition, position);
        if (!CanRunPartitionRepairs(requested: true)) return;
        var current = _partitionRepairs[partition];
        if (current.Waiting.Length > 0) return;

        _partitionRepairs.Remove(partition);
        State.ReplayingPartitions.Remove(partition);
        _partitionHoldsVersion++;
        RecordedHandlingState();
        try
        {
            await PersistPartitionRepairs();
        }
        catch
        {
            _partitionRepairs[partition] = current;
            HoldPartitionForRepair(partition);
            throw;
        }
        Skips.Release(partition, ownership);
        var lastCovered = position.IsActualValue && position > EventSequenceNumber.First ? position - 1UL : EventSequenceNumber.Unavailable;
        CompleteHeldBackDelivery(partition, lastCovered);
        await EvaluateAndRepairMissedDeliveries();
    }
}
