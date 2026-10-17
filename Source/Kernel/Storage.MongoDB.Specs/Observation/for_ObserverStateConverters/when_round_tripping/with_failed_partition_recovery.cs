// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateConverters.when_round_tripping;

public class with_failed_partition_recovery : Specification
{
    FailedPartition _expected;
    FailedPartition _restored;

    void Establish()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(FailedPartition)))
        {
            BsonClassMap.RegisterClassMap<FailedPartition>(new FailedPartitionClassMap().Configure);
        }

        _expected = new()
        {
            ObserverId = "observer",
            Partition = "partition",
            RecoveryCursor = new(new(51), FailedPartitionRecoveryMode.Replay, new(80)),
            RecoveryOwner = new(Guid.NewGuid(), Guid.NewGuid(), 3, FailedPartitionRecoveryState.Resuming, 7),
            Hold = new(FailedPartitionHoldReason.WaitingForHandler, new("event-a"), new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero))
        };
    }

    void Because()
    {
        var joined = new ObserverStateWithFailedPartitions { Id = "observer", FailedPartitions = [_expected] };
        _restored = BsonSerializer.Deserialize<ObserverStateWithFailedPartitions>(joined.ToBson()).ToKernel().FailedPartitions.Single();
    }

    [Fact] void should_keep_the_exact_cursor_and_replay_boundary() => _restored.RecoveryCursor.ShouldEqual(_expected.RecoveryCursor);
    [Fact] void should_keep_the_whole_recovery_owner() => _restored.RecoveryOwner.ShouldEqual(_expected.RecoveryOwner);
    [Fact] void should_keep_the_hold_reason_type_and_start() => _restored.Hold.ShouldEqual(_expected.Hold);
    [Fact] void should_not_add_an_attempt_for_the_hold() => _restored.Attempts.ShouldBeEmpty();
    [Fact] void should_not_change_the_retry_budget() => _restored.AttemptsInCurrentBudget.ShouldEqual(0);
    [Fact] void should_not_quarantine_the_hold() => _restored.IsQuarantined.ShouldBeFalse();
}
