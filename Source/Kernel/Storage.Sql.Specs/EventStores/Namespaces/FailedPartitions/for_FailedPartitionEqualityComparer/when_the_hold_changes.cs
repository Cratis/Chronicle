// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.FailedPartitions.for_FailedPartitionEqualityComparer;

public class when_the_hold_changes : Specification
{
    bool _equal;

    void Because()
    {
        var previous = new Concepts.Observation.FailedPartition { Hold = new(FailedPartitionHoldReason.WaitingForHandler, new("event-a"), new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero)) };
        var current = new Concepts.Observation.FailedPartition { Id = previous.Id, Hold = previous.Hold with { AwaitedEventType = new("event-b") } };
        _equal = FailedPartitionEqualityComparer.Instance.Equals(previous, current);
    }

    [Fact] void should_detect_the_change() => _equal.ShouldBeFalse();
}
