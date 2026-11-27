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

    /// <summary>
    /// Gets a value indicating whether accepted repairs or their durable acknowledgements hold the delivery barrier.
    /// </summary>
    internal bool HasCatchUpRepairBarrier => PendingCatchUpRepairs.Count > 0 || _handoverRepairAcknowledgementsInFlight > 0;

    IReadOnlyList<PendingCatchUpRepair> PendingCatchUpRepairs => State.PendingCatchUpRepairs ?? [];

    bool HasAcceptedCatchUpWork => _catchUpContinuationPending || HasCatchUpRepairBarrier;

    bool HasAcceptedCatchUpActivity => HasAcceptedCatchUpWork || _catchUpContinuationInProgress;

    /// <summary>
    /// Gets a value indicating whether acquisition or another catch-up turn is still active.
    /// </summary>
    /// <remarks>
    /// Timer and watchdog turns do not interleave. A failed transition hook leaves its flag set;
    /// treating that flag as activity would strand acceptance after a failed replay start.
    /// </remarks>
    bool CatchUpContinuationBusy =>
        _catchUpContinuationInProgress || _catchUpHandoversInFlight > 0 ||
        _pendingCatchUpAcquisition is not null;

    bool CanRunCatchUpRepairs =>
        !State.IsReplaying && !IsRetired && !_removed && _continuationLifetime.Capture().IsCurrent;

    bool CatchUpAcceptanceInvalidated => _disposed || CatchUpObserverRemoved || !_observerExists || !KeyOwnsDefinition;

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
        if (HasCatchUpRepairBarrier)
        {
            // The frozen cursor covers the suffix; only the bounded obligations own earlier events.
            _heldBackDeliveries.Clear();
            _missedDeliveries.Clear();
        }
    }

    bool PartitionIsCatchingUp(Concepts.Keys.Key partition) =>
        State.CatchingUpPartitions.Contains(partition) || PendingCatchUpRepairs.Any(_ => _.Partition == partition);

    bool NeedsDurableCatchUpRepair(Concepts.Keys.Key partition, Concepts.Events.EventSequenceNumber from) =>
        from.IsActualValue && from < State.NextEventSequenceNumber && !MissedDeliveryAlreadyOwned(partition);

    void CompletePendingCatchUpRepair(Concepts.Keys.Key partition) =>
        State = State with { PendingCatchUpRepairs = PendingCatchUpRepairs.Where(_ => _.Partition != partition).ToArray() };

    async Task ConfirmCatchUpAcceptance()
    {
        while (!await WriteStateAndConfirm())
        {
            if (CatchUpAcceptanceInvalidated)
            {
                throw new InvalidOperationException("The catch-up handover could not be durably accepted.");
            }

            // Wait only for the subscriber's write suspension, never the whole delivery or successor routing.
            // Those can themselves be waiting for this durable acknowledgement.
            if (_stateWritesResumed is { } resumed) await resumed.Task;
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
        _catchUpHandoversInFlight++;
        _catchUpHandoversSettled ??= new(TaskCreationOptions.RunContinuationsAsynchronously);
        var version = _catchUpContinuationVersion;
        try
        {
            if (await ApplyPendingRepairQuarantine()) return;
            await AcceptNewCatchUpRepairs();
            if (await ContinuePendingRepairs()) return;
            if (!_catchUpContinuationPending) return;
            await ContinueAcceptedSuccessor(version);
        }
        finally
        {
            FinishCatchUpContinuation();
        }
    }

    async Task AcceptNewCatchUpRepairs()
    {
        EvaluateSuspectedSkips();
        var acceptedRepairs = PendingCatchUpRepairs;
        QueueUnownedHeldBackDeliveries();
        AcceptPendingCatchUpRepairs();
        if (!acceptedRepairs.SequenceEqual(PendingCatchUpRepairs)) await ConfirmCatchUpAcceptance();
    }

    async Task ContinueAcceptedSuccessor(int version)
    {
        await RepairMissedDeliveries();
        var madeProgress = _acceptedCatchUpMadeProgress;
        await ContinueAfterCatchUpLap(await NoteCatchUpLap(madeProgress));
        if (version == _catchUpContinuationVersion)
        {
            _catchUpContinuationPending = false;
            _acceptedCatchUpMadeProgress = false;
        }
    }

    void FinishCatchUpContinuation()
    {
        _catchUpContinuationInProgress = false;
        if (--_catchUpHandoversInFlight == 0 && _catchUpHandoversSettled is { } settled)
        {
            _catchUpHandoversSettled = null;
            settled.TrySetResult();
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

    async Task<bool> ContinuePendingRepairs()
    {
        if (!HasCatchUpRepairBarrier) return false;
        await ResumePendingCatchUpRepairs();
        return true;
    }

    async Task ResumePendingCatchUpRepairs()
    {
        if (!CanRunCatchUpRepairs) return;
        foreach (var repair in PendingCatchUpRepairs.ToArray())
        {
            await ResumePendingCatchUpRepair(repair);
        }
    }

    async Task ResumePendingCatchUpRepair(PendingCatchUpRepair repair)
    {
        if (!PendingCatchUpRepairs.Contains(repair)) return;
        if (failures.State.IsFailed(repair.Partition))
        {
            await DischargeFailedCatchUpRepair(repair);
            return;
        }
        if (IsWithoutClient) return;
        await StartPendingCatchUpRepair(repair);
    }

    async Task DischargeFailedCatchUpRepair(PendingCatchUpRepair repair)
    {
        if (await RecordHandoverRepairCompletion(repair.Partition, repair.From, Concepts.Events.EventSequenceNumber.Unavailable, succeeded: false))
        {
            ScheduleCatchUpContinuation(madeProgress: false);
        }
    }

    async Task StartPendingCatchUpRepair(PendingCatchUpRepair repair)
    {
        var request = new CatchUpObserverPartitionRequest(_observerKey, Definition.Type, repair.Partition, repair.From, Definition.EventTypes)
        {
            ReaderScope = ReaderScope,
            IsHandoverRepair = true,
            ToSequenceNumber = repair.To - 1
        };
        var checkpoint = _continuationLifetime.Capture();
        var starter = new ObserverJobStarter<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>
        {
            JobsManager = _jobsManager,
            Logger = logger,
            CreateRequest = () => request,
            RequestPredicate = existing => existing.IsHandoverRepair && existing.Key == repair.Partition && existing.FromSequenceNumber == repair.From && existing.ToSequenceNumber == request.ToSequenceNumber,
            OnAlreadyRunningJob = () => Task.CompletedTask,
            OnResume = () => Task.CompletedTask,
            OnStartNew = () => Task.CompletedTask,
            OnResumeRefused = () => Task.CompletedTask,
            IsObsolete = IsObsolete,
            CanAcquire = () => checkpoint.IsCurrent && !CatchUpAcceptanceInvalidated && PendingCatchUpRepairs.Contains(repair),
            StampBeforeStart = pending => StampCatchUpRepair(pending, repair)
        };
        await checkpoint.Await(starter.Run);
    }
}
