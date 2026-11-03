// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_FailedPartitionGroupConverters;

public class when_ordering_groups : Specification
{
    string[] _result;

    void Because() => _result =
    [
        .. new[]
        {
            FailedPartitions.With("small-old", "a", (FailureKind.Handling, 1)),
            FailedPartitions.With("small-new", "a", (FailureKind.Handling, 50)),
            FailedPartitions.With("large", "a", (FailureKind.Handling, 2)),
            FailedPartitions.With("large", "b", (FailureKind.Handling, 3))
        }.ToGroups().Select(_ => _.ObserverId)
    ];

    [Fact] void should_put_the_largest_group_first() => _result[0].ShouldEqual("large");
    [Fact] void should_order_equal_sized_groups_by_most_recent_failure() => _result.Skip(1).ShouldEqual(["small-new", "small-old"]);
}
