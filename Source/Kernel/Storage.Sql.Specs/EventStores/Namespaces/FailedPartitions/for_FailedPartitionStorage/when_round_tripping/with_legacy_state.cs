// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

using KernelFailedPartition = Cratis.Chronicle.Concepts.Observation.FailedPartition;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.FailedPartitions.for_FailedPartitionStorage.when_round_tripping;

public class with_legacy_state : given.a_failed_partition_storage
{
    KernelFailedPartition _restored;

    async Task Establish()
    {
        await using var context = CreateContext();
        context.FailedPartitions.Add(new()
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ObserverId = "observer",
            Partition = "partition",
            StateJson = """
                {"Attempts":[{"SequenceNumber":{"Value":42}}],"AttemptsBeforeBudgetReset":0,"IsQuarantined":false}
                """
        });
        await context.SaveChangesAsync();
    }

    async Task Because() => _restored = (await _storage.GetFor((ObserverId)"observer")).Partitions.Single();

    [Fact] void should_not_initialize_a_cursor() => _restored.RecoveryCursor.ShouldBeNull();
    [Fact] void should_not_assign_an_owner() => _restored.RecoveryOwner.ShouldBeNull();
    [Fact] void should_not_invent_a_hold() => _restored.Hold.ShouldBeNull();
    [Fact] void should_resume_at_the_last_attempt() => _restored.EffectiveRecoveryCursor.Position.Value.ShouldEqual(42UL);
    [Fact] void should_interpret_legacy_recovery_as_live() => _restored.EffectiveRecoveryCursor.Mode.ShouldEqual(FailedPartitionRecoveryMode.Live);
    [Fact] void should_have_no_replay_boundary() => _restored.EffectiveRecoveryCursor.ReplayBoundary.ShouldBeNull();
    [Fact] void should_keep_the_retry_budget() => _restored.AttemptsInCurrentBudget.ShouldEqual(1);
    [Fact] void should_keep_the_quarantine_default() => _restored.IsQuarantined.ShouldBeFalse();
    [Fact] void should_keep_the_legacy_failure_kind() => _restored.LastAttempt.Kind.ShouldEqual(FailureKind.Unknown);
}
