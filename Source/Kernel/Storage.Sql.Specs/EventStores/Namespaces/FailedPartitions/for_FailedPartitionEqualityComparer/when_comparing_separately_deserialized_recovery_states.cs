// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.FailedPartitions.for_FailedPartitionEqualityComparer;

public class when_comparing_separately_deserialized_recovery_states : Specification
{
    Concepts.Observation.FailedPartition _previous;
    Concepts.Observation.FailedPartition _current;
    bool _equal;

    void Establish()
    {
        var entity = new Concepts.Observation.FailedPartition
        {
            ObserverId = "observer",
            Partition = "partition",
            RecoveryCursor = new(new(51), FailedPartitionRecoveryMode.Replay, new(80)),
            RecoveryOwner = new(Guid.NewGuid(), Guid.NewGuid(), 3, FailedPartitionRecoveryState.Resuming, 7),
            Hold = new(FailedPartitionHoldReason.WaitingForHandler, new("event-a"), new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero))
        }.ToEntity();
        _previous = entity.ToFailedPartition();
        _current = entity.ToFailedPartition();
    }

    void Because() => _equal = FailedPartitionEqualityComparer.Instance.Equals(_previous, _current);

    [Fact] void should_compare_equal() => _equal.ShouldBeTrue();
    [Fact] void should_deserialize_independent_cursors() => _current.RecoveryCursor.ShouldNotBeSame(_previous.RecoveryCursor);
    [Fact] void should_deserialize_independent_owners() => _current.RecoveryOwner.ShouldNotBeSame(_previous.RecoveryOwner);
    [Fact] void should_deserialize_independent_holds() => _current.Hold.ShouldNotBeSame(_previous.Hold);
}
