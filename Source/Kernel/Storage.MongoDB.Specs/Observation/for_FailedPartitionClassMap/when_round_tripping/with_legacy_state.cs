// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_FailedPartitionClassMap.when_round_tripping;

public class with_legacy_state : Specification
{
    FailedPartition _restored;
    BsonDocument _saved;

    void Establish()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(FailedPartition)))
        {
            BsonClassMap.RegisterClassMap<FailedPartition>(new FailedPartitionClassMap().Configure);
        }
    }

    void Because()
    {
        var legacy = new FailedPartition { ObserverId = "observer", Partition = "partition", Attempts = [new() { SequenceNumber = new(42) }] }.ToBsonDocument();
        _restored = BsonSerializer.Deserialize<FailedPartition>(legacy);
        _saved = _restored.ToBsonDocument();
    }

    [Fact] void should_not_invent_a_deferral() => _restored.Deferral.ShouldBeNull();
    [Fact] void should_not_write_a_deferral() => _saved.Names.Any(name => name.Equals("deferral", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
    [Fact] void should_not_initialize_a_cursor() => _restored.RecoveryCursor.ShouldBeNull();
    [Fact] void should_not_assign_an_owner() => _restored.RecoveryOwner.ShouldBeNull();
    [Fact] void should_not_invent_a_hold() => _restored.Hold.ShouldBeNull();
    [Fact] void should_resume_at_the_last_attempt() => _restored.EffectiveRecoveryCursor.Position.Value.ShouldEqual(42UL);
    [Fact] void should_interpret_legacy_recovery_as_live() => _restored.EffectiveRecoveryCursor.Mode.ShouldEqual(FailedPartitionRecoveryMode.Live);
    [Fact] void should_have_no_replay_boundary() => _restored.EffectiveRecoveryCursor.ReplayBoundary.ShouldBeNull();
    [Fact] void should_keep_the_retry_budget() => _restored.AttemptsInCurrentBudget.ShouldEqual(1);
    [Fact] void should_keep_the_quarantine_default() => _restored.IsQuarantined.ShouldBeFalse();
    [Fact] void should_not_write_recovery_fields_for_legacy_state() => _saved.Names.Any(name => name.StartsWith("recovery", StringComparison.OrdinalIgnoreCase) || name.Equals("hold", StringComparison.OrdinalIgnoreCase) || name.Equals("effectiveRecoveryCursor", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
}
