// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.FailedPartitions.for_FailedPartitionEqualityComparer;

public class when_the_retry_budget_is_reset : Specification
{
    bool _equal;

    void Because()
    {
        var previous = new Concepts.Observation.FailedPartition { IsQuarantined = true };
        var current = new Concepts.Observation.FailedPartition
        {
            Id = previous.Id,
            IsQuarantined = true,
            AttemptsBeforeBudgetReset = 3
        };
        _equal = FailedPartitionEqualityComparer.Instance.Equals(previous, current);
    }

    [Fact] void should_emit_the_changed_snapshot() => _equal.ShouldBeFalse();
}
