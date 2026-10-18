// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.FailedPartitions.for_FailedPartitionEqualityComparer;

public class when_the_recovery_owner_changes : Specification
{
    bool _equal;

    void Because()
    {
        var previous = new Concepts.Observation.FailedPartition { RecoveryOwner = new(Guid.NewGuid(), Guid.NewGuid(), 2, FailedPartitionRecoveryState.Retrying, 7) };
        var current = new Concepts.Observation.FailedPartition { Id = previous.Id, RecoveryOwner = previous.RecoveryOwner with { Execution = 3 } };
        _equal = FailedPartitionEqualityComparer.Instance.Equals(previous, current);
    }

    [Fact] void should_detect_the_change() => _equal.ShouldBeFalse();
}
