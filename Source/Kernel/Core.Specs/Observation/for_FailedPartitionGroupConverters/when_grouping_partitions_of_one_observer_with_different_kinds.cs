// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_FailedPartitionGroupConverters;

public class when_grouping_partitions_of_one_observer_with_different_kinds : Specification
{
    FailedPartitionGroup[] _result;

    void Because() => _result =
    [
        .. new[]
        {
            FailedPartitions.With("observer", "a", (FailureKind.Handling, 1)),
            FailedPartitions.With("observer", "b", (FailureKind.Handling, 2)),
            FailedPartitions.With("observer", "c", (FailureKind.Timeout, 3))
        }.ToGroups()
    ];

    [Fact] void should_have_a_group_per_kind() => _result.Length.ShouldEqual(2);
    [Fact] void should_count_the_handling_partitions() => _result.Single(_ => _.Kind == FailureKind.Handling).PartitionCount.ShouldEqual(2);
    [Fact] void should_count_the_timeout_partitions() => _result.Single(_ => _.Kind == FailureKind.Timeout).PartitionCount.ShouldEqual(1);
    [Fact] void should_identify_the_group_by_observer_and_kind() => _result.Select(_ => _.Id).ShouldContainOnly("observer/Handling", "observer/Timeout");
}
