// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.FailedPartitions.for_FailedPartitionEqualityComparer;

public class when_an_attempts_failure_kind_changes : Specification
{
    bool _equal;

    void Because()
    {
        var attempt = new FailedPartitionAttempt();
        var previous = new Concepts.Observation.FailedPartition { Attempts = [attempt] };
        var current = new Concepts.Observation.FailedPartition
        {
            Id = previous.Id,
            Attempts = [attempt with { Kind = FailureKind.Handling }]
        };
        _equal = FailedPartitionEqualityComparer.Instance.Equals(previous, current);
    }

    [Fact] void should_emit_the_changed_snapshot() => _equal.ShouldBeFalse();
}
