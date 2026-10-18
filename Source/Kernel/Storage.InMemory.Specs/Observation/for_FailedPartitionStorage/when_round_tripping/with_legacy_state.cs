// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_FailedPartitionStorage.when_round_tripping;

public class with_legacy_state : Specification
{
    FailedPartitionStorage _storage;
    FailedPartition _restored;

    void Establish() => _storage = new();

    async Task Because()
    {
        var partition = new FailedPartition { ObserverId = "observer", Partition = "partition", Attempts = [new() { SequenceNumber = new(42) }] };
        await _storage.Save("observer", new() { Partitions = [partition] });
        _restored = (await _storage.GetFor((ObserverId)"observer")).Partitions.Single();
    }

    [Fact] void should_not_initialize_a_cursor() => _restored.RecoveryCursor.ShouldBeNull();
    [Fact] void should_not_assign_an_owner() => _restored.RecoveryOwner.ShouldBeNull();
    [Fact] void should_not_invent_a_hold() => _restored.Hold.ShouldBeNull();
    [Fact] void should_resume_at_the_last_attempt() => _restored.EffectiveRecoveryCursor.Position.Value.ShouldEqual(42UL);
    [Fact] void should_interpret_legacy_recovery_as_live() => _restored.EffectiveRecoveryCursor.Mode.ShouldEqual(FailedPartitionRecoveryMode.Live);
    [Fact] void should_have_no_replay_boundary() => _restored.EffectiveRecoveryCursor.ReplayBoundary.ShouldBeNull();
    [Fact] void should_keep_the_retry_budget() => _restored.AttemptsInCurrentBudget.ShouldEqual(1);
    [Fact] void should_keep_the_quarantine_default() => _restored.IsQuarantined.ShouldBeFalse();

    void Destroy() => _storage.Dispose();
}
