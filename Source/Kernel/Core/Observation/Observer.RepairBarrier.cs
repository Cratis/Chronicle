// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    int _handoverRepairAcknowledgementsInFlight;
    bool _repairQuarantinePending;
    EventSequenceNumber? _repairAcceptanceCursorCap;

    /// <inheritdoc/>
    public async Task HandoverRepairCaughtUp(Key partition, EventSequenceNumber from, EventSequenceNumber to, EventSequenceNumber lastHandled, bool succeeded, ulong? ownershipEpoch = null)
    {
        if (CatchUpAcceptanceInvalidated || IgnoresStaleRelease(ClaimHolderKind.PartitionCatchUp, partition, ownershipEpoch) || !PendingCatchUpRepairs.Any(_ => _.Partition == partition && _.From == from && _.To == to)) return;
        var version = _catchUpContinuationVersion;
        var accepted = await RecordHandoverRepairCompletion(partition, from, lastHandled, succeeded);
        if (accepted && version == _catchUpContinuationVersion && !CatchUpAcceptanceInvalidated) ScheduleCatchUpContinuation(madeProgress: succeeded);
    }

    [LoggerMessage(LogLevel.Debug, "Live delivery held behind the durable handover repair barrier")]
    static partial void RepairBarrierHoldsLiveDelivery(ILogger logger);

    async Task<bool> RecordHandoverRepairCompletion(Key partition, EventSequenceNumber from, EventSequenceNumber lastHandled, bool succeeded)
    {
        var repair = PendingCatchUpRepairs.SingleOrDefault(_ => _.Partition == partition && _.From == from);
        if (repair is null || CatchUpAcceptanceInvalidated) return false;
        _handoverRepairAcknowledgementsInFlight++;
        try
        {
            return await PersistHandoverRepairCompletion(repair, lastHandled, succeeded);
        }
        finally
        {
            _handoverRepairAcknowledgementsInFlight--;
        }
    }

    async Task<bool> PersistHandoverRepairCompletion(PendingCatchUpRepair repair, EventSequenceNumber lastHandled, bool succeeded)
    {
        var partition = repair.Partition;
        if (!succeeded && !failures.State.IsFailed(partition))
        {
            var failedAt = lastHandled.IsActualValue && lastHandled >= repair.From ? lastHandled.Next() : repair.From;
            await RecordPartitionFailure(partition, failedAt, ["Handover repair failed"], string.Empty, FailureKind.Unknown, null, null, new(WebhookDeliveryFailurePolicy.Retry), deferQuarantine: true);
        }
        if (CatchUpAcceptanceInvalidated || !PendingCatchUpRepairs.Contains(repair)) return false;
        Skips.Completing(partition, repair.To - 1);
        CompletePendingCatchUpRepair(partition);
        _partitionHoldsVersion++;
        ShadowHandOverPartition(partition, ClaimHolderKind.PartitionCatchUp);
        State = State with { CatchingUpPartitions = new HashSet<Key>(State.CatchingUpPartitions.Where(_ => _ != partition)) };
        await ConfirmCatchUpAcceptance();
        return !CatchUpAcceptanceInvalidated;
    }

    EventSequenceNumber CursorDuringRepairAcceptance(EventSequenceNumber candidate)
    {
        if (_catchUpConclusionsInFlight == 0) return State.NextEventSequenceNumber;

        // The suffix left to the successor has no other owner: the cursor may reach it, never pass it.
        if (_repairAcceptanceCursorCap is { } cap && cap < candidate) candidate = cap;
        if (candidate <= State.NextEventSequenceNumber) return State.NextEventSequenceNumber;

        // A delivery admitted before acceptance can finish while the acknowledgement waits for its write suspension.
        // Freeze at that final durable watermark, extending the obligations atomically with the live cursor write.
        State = State with { PendingCatchUpRepairs = PendingCatchUpRepairs.Select(_ => _ with { To = candidate }).ToArray() };
        return candidate;
    }

    void CapCursorAtSuffixLeftToSuccessor()
    {
        // Accepting the barrier forgets these deliveries because the frozen cursor leaves them to the successor.
        // A partition with an obligation is excluded: its bounded range is extended together with the cursor.
        var next = State.NextEventSequenceNumber;
        var heldBack = _heldBackDeliveries
            .Where(_ => _.Value.Last >= next && !PendingCatchUpRepairs.Any(repair => repair.Partition == _.Key))
            .Select(_ => _.Value.First < next ? next : _.Value.First);
        foreach (var first in heldBack.Concat(_missedDeliveries.Values.Where(_ => _.IsActualValue && _ >= next)))
        {
            if (_repairAcceptanceCursorCap is not { } cap || first < cap) _repairAcceptanceCursorCap = first;
        }
    }

    void ReleaseRepairAcceptanceCursorCap() => _repairAcceptanceCursorCap = null;

    Task ApplyFailureQuarantine(bool deferQuarantine)
    {
        ArmWebhookProbeAfterFailure();
        if (!deferQuarantine) return TransitionTo<QuarantinedObserver>();
        _repairQuarantinePending = true;
        return Task.CompletedTask;
    }

    async Task<bool> ApplyPendingRepairQuarantine()
    {
        if (!_repairQuarantinePending) return false;
        _repairQuarantinePending = false;
        if (!ShouldQuarantineObserver(await GetRetryConfiguration())) return false;
        await TransitionTo<QuarantinedObserver>();
        return true;
    }

    bool HoldLiveDeliveryForRepair()
    {
        if (!HasCatchUpRepairBarrier && _catchUpConclusionsInFlight == 0) return false;
        RepairBarrierHoldsLiveDelivery(logger);
        return true;
    }
}
