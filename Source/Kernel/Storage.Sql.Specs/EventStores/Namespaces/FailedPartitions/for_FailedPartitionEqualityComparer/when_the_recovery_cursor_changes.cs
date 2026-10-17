// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.FailedPartitions.for_FailedPartitionEqualityComparer;

public class when_the_recovery_cursor_changes : Specification
{
    bool _equal;

    void Because()
    {
        var previous = new Concepts.Observation.FailedPartition { RecoveryCursor = new(new(50), FailedPartitionRecoveryMode.Replay, new(80)) };
        var current = new Concepts.Observation.FailedPartition { Id = previous.Id, RecoveryCursor = previous.RecoveryCursor with { Position = new(51) } };
        _equal = FailedPartitionEqualityComparer.Instance.Equals(previous, current);
    }

    [Fact] void should_detect_the_change() => _equal.ShouldBeFalse();
}
