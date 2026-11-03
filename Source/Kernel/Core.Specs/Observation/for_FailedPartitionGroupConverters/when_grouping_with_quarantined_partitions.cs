// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_FailedPartitionGroupConverters;

public class when_grouping_with_quarantined_partitions : Specification
{
    FailedPartitionGroup _result;

    void Because()
    {
        var quarantined = FailedPartitions.With("observer", "quarantined", (FailureKind.Handling, 1));
        quarantined.IsQuarantined = true;
        var retrying = FailedPartitions.With("observer", "retrying", (FailureKind.Handling, 2));
        _result = new[] { quarantined, retrying }.ToGroups().Single();
    }

    [Fact] void should_count_both_partitions() => _result.PartitionCount.ShouldEqual(2);
    [Fact] void should_count_the_quarantined_partition() => _result.QuarantinedCount.ShouldEqual(1);
}
