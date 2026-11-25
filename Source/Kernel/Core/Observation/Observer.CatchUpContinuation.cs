// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    IGrainTimer? _catchUpContinuationTimer;
    bool _catchUpContinuationPending;
    bool _catchUpContinuationInProgress;
    bool _acceptedCatchUpMadeProgress;
    int _catchUpContinuationVersion;

    IReadOnlyList<PendingCatchUpRepair> PendingCatchUpRepairs => State.PendingCatchUpRepairs ?? [];

    bool HasAcceptedCatchUpWork => _catchUpContinuationPending || PendingCatchUpRepairs.Count > 0;

    bool HasAcceptedCatchUpActivity => HasAcceptedCatchUpWork || _catchUpContinuationInProgress;

    bool CatchUpContinuationBusy =>
        _catchUpContinuationInProgress || _catchUpHandoversInFlight > 0 ||
        _pendingCatchUpAcquisition is not null || _stateTransitionInProgress;

    bool CanRunCatchUpRepairs =>
        !State.IsReplaying && !IsRetired && !_removed && _continuationLifetime.Capture().IsCurrent;

    void AcceptPendingCatchUpRepairs()
    {
        var repairs = PendingCatchUpRepairs.ToDictionary(_ => _.Partition);
        foreach (var (partition, from) in _missedDeliveries.ToArray())
        {
            if (!NeedsDurableCatchUpRepair(partition, from)) continue;
            repairs[partition] = new(partition, from, State.NextEventSequenceNumber);
            _missedDeliveries.Remove(partition);
        }

        State = State with { PendingCatchUpRepairs = repairs.Values.ToArray() };
    }

    bool PartitionIsCatchingUp(Concepts.Keys.Key partition) =>
        State.CatchingUpPartitions.Contains(partition) || PendingCatchUpRepairs.Any(_ => _.Partition == partition);

    bool NeedsDurableCatchUpRepair(Concepts.Keys.Key partition, Concepts.Events.EventSequenceNumber from) =>
        from.IsActualValue && from < State.NextEventSequenceNumber && !MissedDeliveryAlreadyOwned(partition);

    void CompletePendingCatchUpRepair(Concepts.Keys.Key partition)
    {
        var repairs = PendingCatchUpRepairs.Where(_ => _.Partition != partition).ToList();
        if (_heldBackDeliveries.TryGetValue(partition, out var held))
        {
            var from = held.First;
            var to = from >= State.NextEventSequenceNumber ? from.Next() : State.NextEventSequenceNumber;
            repairs.Add(new(partition, from, to));
            _missedDeliveries.Remove(partition);
        }
        State = State with { PendingCatchUpRepairs = repairs.ToArray() };
    }

    async Task ConfirmCatchUpAcceptance()
    {
        if (!await WriteStateAndConfirm())
        {
            throw new InvalidOperationException("The catch-up handover could not be durably accepted.");
        }
    }

    void ScheduleCatchUpContinuation(bool madeProgress)
    {
        _catchUpContinuationPending = true;
        _catchUpContinuationVersion++;
        _acceptedCatchUpMadeProgress |= madeProgress;
        _catchUpContinuationTimer ??= this.RegisterGrainTimer(
            async _ =>
            {
                _catchUpContinuationTimer?.Dispose();
                _catchUpContinuationTimer = null;
                await ContinueAcceptedCatchUp();
            },
            new GrainTimerCreationOptions
            {
                DueTime = TimeSpan.Zero,
                Period = Timeout.InfiniteTimeSpan,
                Interleave = false
            });
    }

    async Task ContinueAcceptedCatchUp()
    {
        if (!CanContinueAcceptedCatchUp()) return;
        _catchUpContinuationInProgress = true;
        var version = _catchUpContinuationVersion;
        try
        {
            QueueUnownedHeldBackDeliveries();
            AcceptPendingCatchUpRepairs();
            await ConfirmCatchUpAcceptance();
            await ResumePendingCatchUpRepairs();
            if (!_catchUpContinuationPending) return;
            await RepairMissedDeliveries();
            var madeProgress = _acceptedCatchUpMadeProgress;
            await ContinueAfterCatchUpLap(await NoteCatchUpLap(madeProgress));
            if (version == _catchUpContinuationVersion)
            {
                _catchUpContinuationPending = false;
                _acceptedCatchUpMadeProgress = false;
            }
        }
        finally
        {
            _catchUpContinuationInProgress = false;
        }
    }

    bool CanContinueAcceptedCatchUp() =>
        HasAcceptedCatchUpWork && !CatchUpContinuationBusy && CanRunCatchUpRepairs;

    async Task<CatchUpObserverPartitionRequest> StampCatchUpRepair(CatchUpObserverPartitionRequest request, PendingCatchUpRepair repair)
    {
        var epochs = await GrantPartitionsBeforeStart([repair.Partition], ClaimHolderKind.PartitionCatchUp, repair.From);
        await ConfirmCatchUpAcceptance();
        return request with { OwnershipEpoch = epochs[repair.Partition] };
    }

    async Task ResumePendingCatchUpRepairs()
    {
        if (IsWithoutClient || !CanRunCatchUpRepairs) return;
        foreach (var repair in PendingCatchUpRepairs.ToArray())
        {
            if (failures.State.IsFailed(repair.Partition)) continue;
            await StartPendingCatchUpRepair(repair);
        }
    }

    async Task StartPendingCatchUpRepair(PendingCatchUpRepair repair)
    {
        var request = new CatchUpObserverPartitionRequest(_observerKey, Definition.Type, repair.Partition, repair.From, Definition.EventTypes)
        {
            ReaderScope = ReaderScope,
            IsHandoverRepair = true
        };
        var checkpoint = _continuationLifetime.Capture();
        var starter = new ObserverJobStarter<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>
        {
            JobsManager = _jobsManager,
            Logger = logger,
            CreateRequest = () => request,
            RequestPredicate = existing => existing.IsHandoverRepair && existing.Key == repair.Partition && existing.FromSequenceNumber == repair.From,
            OnAlreadyRunningJob = () => Task.CompletedTask,
            OnResume = () => Task.CompletedTask,
            OnStartNew = () => Task.CompletedTask,
            OnResumeRefused = () => Task.CompletedTask,
            IsObsolete = IsObsolete,
            StampBeforeStart = pending => StampCatchUpRepair(pending, repair)
        };
        await checkpoint.Await(starter.Run);
    }
}
