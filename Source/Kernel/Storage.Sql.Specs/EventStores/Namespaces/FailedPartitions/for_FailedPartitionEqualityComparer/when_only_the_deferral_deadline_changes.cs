// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.FailedPartitions.for_FailedPartitionEqualityComparer;

public class when_only_the_deferral_deadline_changes : Specification
{
    bool _equal;

    void Because()
    {
        var since = new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);
        var previous = new Concepts.Observation.FailedPartition { Deferral = new(Guid.NewGuid(), PartitionDeferralReason.RetryAfter, since, since.AddMinutes(1), new(42)) };
        var current = new Concepts.Observation.FailedPartition { Id = previous.Id, Deferral = previous.Deferral with { Until = since.AddMinutes(3) } };
        _equal = FailedPartitionEqualityComparer.Instance.Equals(previous, current);
    }

    [Fact] void should_detect_the_change() => _equal.ShouldBeFalse();
}
