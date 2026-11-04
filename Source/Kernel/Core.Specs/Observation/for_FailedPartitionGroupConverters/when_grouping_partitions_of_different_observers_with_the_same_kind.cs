// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_FailedPartitionGroupConverters;

public class when_grouping_partitions_of_different_observers_with_the_same_kind : Specification
{
    FailedPartitionGroup[] _result;

    void Because() => _result =
    [
        .. new[]
        {
            FailedPartitions.With("first", "a", (FailureKind.Handling, 1)),
            FailedPartitions.With("second", "a", (FailureKind.Handling, 2))
        }.ToGroups()
    ];

    [Fact] void should_have_a_group_per_observer() => _result.Select(_ => _.ObserverId).ShouldContainOnly("first", "second");
    [Fact] void should_count_one_partition_in_each() => _result.All(_ => _.PartitionCount == 1).ShouldBeTrue();
}
