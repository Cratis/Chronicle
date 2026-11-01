// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <inheritdoc/>
    public async Task PartitionHeld(Key partition, FailedPartitionRecoveryCursor cursor, EventTypeId awaitedEventType, FailedPartitionId? expectedEpisodeId = null, Guid? expectedDeferralId = null)
    {
        FailedPartition? resumable = null;
        await _alertMutationLock.WaitAsync();
        try
        {
            if (!IsTypeSetControlled || IsRetired || _removed || IsQuarantined || !KeyOwnsDefinition || !IsCurrentRecovery(partition, expectedEpisodeId, expectedDeferralId)) return;
            var exists = Failures.TryGet(partition, out var failure);
            if (failure?.RecoveryCursor is not null && expectedEpisodeId is null) return;
            failure ??= new() { Partition = partition, ObserverId = _observerId };

            // A hold is not an attempt: nothing failed, the partition waits for a handler that can take the event.
            ForgetRecoveryStart(partition);
            failure.RecoveryCursor = cursor;
            failure.Hold = new(FailedPartitionHoldReason.WaitingForHandler, awaitedEventType, _clock.GetUtcNow());
            if (!exists)
            {
                Failures.Partitions = Failures.Partitions.Append(failure).ToArray();
                State = State with { FailedPartitionCount = State.FailedPartitionCount + 1 };
            }

            ChangeAlertState();
            await WriteFailedPartitions();
            await WriteStateAsync();
            if (_subscription.Targets.Any(target => target.DeclaredEventTypes?.Any(declared => declared.Id == awaitedEventType) == true))
            {
                resumable = failure;
            }
        }
        finally
        {
            _alertMutationLock.Release();
        }

        // A capable target may have joined between the delivery decision and the hold being recorded.
        if (resumable is not null) await TryRecoverFailedPartition(resumable.Partition);
    }
}
