// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

using KernelFailedPartition = Cratis.Chronicle.Concepts.Observation.FailedPartition;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_round_tripping;

public class with_failed_partition_recovery : given.an_observer_state_storage
{
    KernelFailedPartition _partition;
    KernelFailedPartition _restored;

    void Establish()
    {
        _partition = new()
        {
            ObserverId = "observer",
            Partition = "partition",
            RecoveryCursor = new(new(51), FailedPartitionRecoveryMode.Replay, new(80)),
            RecoveryOwner = new(Guid.NewGuid(), Guid.NewGuid(), 3, FailedPartitionRecoveryState.Resuming, 7),
            Hold = new(FailedPartitionHoldReason.WaitingForHandler, new("event-a"), new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero))
        };
    }

    async Task Because()
    {
        await _storage.Save(new() { Identifier = "observer", FailedPartitions = [_partition] });
        _restored = (await _storage.Get("observer")).FailedPartitions.Single();
    }

    [Fact] void should_keep_the_exact_cursor_and_replay_boundary() => _restored.RecoveryCursor.ShouldEqual(_partition.RecoveryCursor);
    [Fact] void should_keep_the_whole_recovery_owner() => _restored.RecoveryOwner.ShouldEqual(_partition.RecoveryOwner);
    [Fact] void should_keep_the_hold_reason_type_and_start() => _restored.Hold.ShouldEqual(_partition.Hold);
    [Fact] void should_not_add_an_attempt_for_the_hold() => _restored.Attempts.ShouldBeEmpty();
    [Fact] void should_not_change_the_retry_budget() => _restored.AttemptsInCurrentBudget.ShouldEqual(0);
    [Fact] void should_keep_quarantine() => _restored.IsQuarantined.ShouldBeFalse();
}
