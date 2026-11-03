// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_FailedPartitionGroupConverters;

public class when_grouping_partitions_without_attempts : Specification
{
    FailedPartitionGroup _result;

    void Because() => _result = new[] { FailedPartitions.With("observer", "a") }.ToGroups().Single();

    [Fact] void should_group_them_under_unknown() => _result.Kind.ShouldEqual(Concepts.Observation.FailureKind.Unknown);
    [Fact] void should_count_the_partition() => _result.PartitionCount.ShouldEqual(1);
    [Fact] void should_not_have_a_first_observed_time() => _result.FirstObserved.ShouldBeNull();
    [Fact] void should_not_have_a_last_observed_time() => _result.LastObserved.ShouldBeNull();
}
