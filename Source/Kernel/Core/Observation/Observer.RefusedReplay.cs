// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <inheritdoc/>
    public async Task ReleaseRefusedReplay()
    {
        if (IsRetired || _removed || !KeyOwnsDefinition || (!State.IsReplaying && State.RunningState != ObserverRunningState.Replaying)) return;

        Skips.Reset();
        State = State with
        {
            IsReplaying = false,
            ReplayingPartitions = new HashSet<Key>(),

            // Full replay supersedes partition catch-ups. Routing clears these markers too and re-evaluates
            // outstanding work from the retained cursor; refusal must not fabricate any completed coverage.
            CatchingUpPartitions = new HashSet<Key>()
        };
        SeedSuspectedSkipOwnership();
        RecordedHandlingState();
        await WriteStateAsync();
        if (!IsQuarantined)
        {
            await TransitionTo<Routing>();
        }
    }

    /// <inheritdoc/>
    public async Task ReleaseRefusedPartitionReplay(Key partition, EventSequenceNumber heldBackFrom)
    {
        if (IsRetired || _removed || !KeyOwnsDefinition || !State.ReplayingPartitions.Contains(partition)) return;

        // Keep ownership until the catch-up decision, but claim no replay coverage or successful delivery.
        Skips.JobStarted(partition);
        var ownership = Skips.OwnershipTicket(partition);
        try
        {
            // Never guess a legacy job's bound from shared progress: other partitions may have advanced it.
            // Persist the quarantined failure before removing the marker, so a crash cannot enable live delivery.
            if (!heldBackFrom.IsActualValue)
            {
                await QuarantineRefusedPartitionWithoutPosition(partition);
            }

            State.ReplayingPartitions.Remove(partition);
            RecordedHandlingState();
            await WriteStateAsync();
            if (heldBackFrom.IsActualValue && !IsQuarantined)
            {
                await ProbeAndStartCatchupJobFrom(partition, heldBackFrom);
            }
        }
        finally
        {
            Skips.Release(partition, ownership);
        }

        EvaluateSuspectedSkips();
    }

    async Task QuarantineRefusedPartitionWithoutPosition(Key partition)
    {
        const string reason = "Refused legacy partition replay has no persisted held-back position. The unchanged sink cannot be caught up safely; reconcile the sink and select a recovery position before clearing partition quarantine.";
        await _alertMutationLock.WaitAsync();
        try
        {
            var alreadyFailed = Failures.TryGet(partition, out var existing);
            var position = alreadyFailed ? existing!.LastAttempt.SequenceNumber : EventSequenceNumber.Unavailable;
            logger.PartitionFailed(partition, position, [reason], string.Empty);
            failures.State.RegisterAttempt(partition, position, [reason], string.Empty);
            failures.State.Quarantine(partition);
            if (!alreadyFailed)
            {
                State = State with { FailedPartitionCount = State.FailedPartitionCount + 1 };
            }

            ChangeAlertState();
            await WriteStateAsync();
            await WriteFailedPartitions();
        }
        finally
        {
            _alertMutationLock.Release();
        }
    }
}
