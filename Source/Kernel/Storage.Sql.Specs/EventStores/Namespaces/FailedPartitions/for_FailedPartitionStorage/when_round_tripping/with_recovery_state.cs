// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

using KernelFailedPartition = Cratis.Chronicle.Concepts.Observation.FailedPartition;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.FailedPartitions.for_FailedPartitionStorage.when_round_tripping;

public class with_recovery_state : given.a_failed_partition_storage
{
    KernelFailedPartition[] _expected;
    KernelFailedPartition[] _restored;

    void Establish()
    {
        _expected = Enum.GetValues<FailedPartitionRecoveryState>().Select(state => new KernelFailedPartition
        {
            ObserverId = "observer",
            Partition = state.ToString(),
            RecoveryCursor = new(new(51), state == FailedPartitionRecoveryState.Waiting ? FailedPartitionRecoveryMode.Live : FailedPartitionRecoveryMode.Replay, state == FailedPartitionRecoveryState.Waiting ? null : new(80)),
            RecoveryOwner = new(Guid.NewGuid(), Guid.NewGuid(), 3, state, 7),
            Hold = new(FailedPartitionHoldReason.WaitingForHandler, new("event-a"), new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero)),
            Attempts = [new() { SequenceNumber = new(42) }, new() { SequenceNumber = new(50), Kind = FailureKind.Handling }],
            AttemptsBeforeBudgetReset = 1,
            IsQuarantined = true
        }).ToArray();
    }

    async Task Because()
    {
        await _storage.Save("observer", new() { Partitions = _expected });
        var stored = (await _storage.GetFor((ObserverId)"observer")).Partitions.ToArray();
        _restored = _expected.Select(expected => stored.Single(partition => partition.Id == expected.Id)).ToArray();
    }

    [Fact] void should_keep_live_and_replay_cursors() => _restored.Select(partition => partition.RecoveryCursor).ShouldEqual(_expected.Select(partition => partition.RecoveryCursor));
    [Fact] void should_keep_each_whole_owner_tuple() => _restored.Select(partition => partition.RecoveryOwner).ShouldEqual(_expected.Select(partition => partition.RecoveryOwner));
    [Fact] void should_keep_each_hold() => _restored.Select(partition => partition.Hold).ShouldEqual(_expected.Select(partition => partition.Hold));
    [Fact] void should_keep_attempts() => _restored.Select(partition => partition.Attempts.Count()).ShouldEqual([2, 2, 2]);
    [Fact] void should_keep_retry_budgets() => _restored.Select(partition => partition.AttemptsInCurrentBudget).ShouldEqual([1, 1, 1]);
    [Fact] void should_keep_quarantine() => _restored.All(partition => partition.IsQuarantined).ShouldBeTrue();
    [Fact] void should_keep_the_last_attempt() => _restored.Select(partition => partition.LastAttempt.SequenceNumber.Value).ShouldEqual([50UL, 50UL, 50UL]);
    [Fact] void should_keep_failure_kinds() => _restored.All(partition => partition.LastAttempt.Kind == FailureKind.Handling).ShouldBeTrue();
}
